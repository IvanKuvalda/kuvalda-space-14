// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.DeadSpace.OrganPrinter;

/// <summary>
/// Поле мини-игры «Сплайсинг ткани»: Qix-подобный захват территории.
/// Игрок-инжектор чертит линии от закрашенной зоны; замкнутый контур затягивается «мясом».
/// Некроз (красные искры) летает по незаполненной области, глитчи (синие пиксели) ползают по границам закрашенного.
/// </summary>
public sealed class SplicingGameControl : Control
{
    private const int FieldW = 46;
    private const int FieldH = 32;
    private const int CellPx = 10;
    private const float TargetCoverage = 0.75f;
    private const int MaxNecrosis = 5;
    private const int MaxGlitches = 2;

    private enum Cell : byte
    {
        Empty = 0,
        Filled = 1,
        Trail = 2,
    }

    private enum Direction
    {
        None = 0,
        Up,
        Down,
        Left,
        Right,
    }

    private sealed class Necrosis
    {
        public float X;
        public float Y;
        public float VX;
        public float VY;
        public float Speed;
        public float Phase;
    }

    private sealed class Glitch
    {
        public int X;
        public int Y;
        public Direction Dir;
        public double NextStep;
    }

    private static readonly (float TrailHit, float PlayerHit) FastDamage = (18f, 26f);
    private static readonly (float TrailHit, float PlayerHit) FineDamage = (10f, 15f);

    private readonly Cell[,] _cells = new Cell[FieldW, FieldH];
    private readonly List<Direction> _held = new();
    private readonly List<(int X, int Y)> _trail = new();
    private readonly List<Necrosis> _necrosis = new();
    private readonly List<Glitch> _glitches = new();
    private readonly IGameTiming _timing;
    private readonly SharedAudioSystem _audio;
    private readonly Random _random = new();

    private bool[,] _silhouette = new bool[FieldW, FieldH];
    private int _silhouetteTotal;
    private int _silhouetteFilled;

    private bool _fastMode = true;
    private bool _running;
    private bool _finished;
    private bool _won;

    private (int X, int Y) _player = (FieldW / 2, 0);
    private (int X, int Y) _respawn = (FieldW / 2, 0);
    private (int X, int Y) _playerFrom = (FieldW / 2, 0);
    private Direction _dir = Direction.Right;
    private bool _drawing;
    private float _stepAccum;
    private float _invuln;
    private float _integrity = 100f;
    private double _coverage;

    private TimeSpan _lastFrame;

    /// <summary>Вызывается при завершении игры: выиграл ли игрок и качество ткани 0-100.</summary>
    public event Action<bool, int>? OnFinished;

    /// <summary>Вызывается при изменении покрытия силуэта (0-100) или целостности принтера.</summary>
    public event Action<int, int>? OnStatsChanged;

    public SplicingGameControl()
    {
        CanKeyboardFocus = true;
        KeyboardFocusOnClick = true;
        MouseFilter = MouseFilterMode.Stop;
        MinSize = new Vector2(FieldW * CellPx + 8, FieldH * CellPx + 8);

        _timing = IoCManager.Resolve<IGameTiming>();
        _audio = IoCManager.Resolve<IEntityManager>().System<SharedAudioSystem>();

        OnKeyBindDown += OnKey;
        OnKeyBindUp += OnKeyRelease;

        ResetField();
    }

    public bool FastMode => _fastMode;
    public int Integrity => (int) _integrity;
    public int Coverage => (int) Math.Round(_coverage * 100f);

    /// <summary>
    /// Загружает силуэт органа для показа на экране выбора режима.
    /// </summary>
    public void LoadSilhouette(EntProtoId product)
    {
        ResetField();
        LoadSilhouetteInner(product);
        RecomputeCoverage();
        EmitStats();
    }

    private void LoadSilhouetteInner(EntProtoId product)
    {
        _silhouette = OrganSilhouettes.GetMask(product, FieldW, FieldH);
        _silhouetteTotal = 0;
        _silhouetteFilled = 0;
        for (var x = 1; x < FieldW - 1; x++)
        {
            for (var y = 1; y < FieldH - 1; y++)
            {
                if (_silhouette[x, y])
                    _silhouetteTotal++;
            }
        }
    }

    public void Start(EntProtoId product, bool fastMode)
    {
        _fastMode = fastMode;
        _held.Clear();
        ResetField();
        LoadSilhouetteInner(product);

        _running = true;
        _finished = false;
        _won = false;
        _integrity = 100f;
        _coverage = 0f;
        _invuln = 0f;
        _lastFrame = _timing.RealTime;

        SpawnNecrosis(2);
        GrabKeyboardFocus();
        EmitStats();
    }

    public void Abort()
    {
        _running = false;
        _finished = true;
        ResetField();
    }

    private void ResetField()
    {
        _player = (FieldW / 2, 0);
        _respawn = _player;
        _playerFrom = _player;
        _dir = Direction.Right;
        _drawing = false;
        _stepAccum = 0f;
        _trail.Clear();
        _necrosis.Clear();
        _glitches.Clear();

        for (var x = 0; x < FieldW; x++)
        {
            for (var y = 0; y < FieldH; y++)
            {
                _cells[x, y] = x == 0 || y == 0 || x == FieldW - 1 || y == FieldH - 1
                    ? Cell.Filled
                    : Cell.Empty;
            }
        }
    }

    private void OnKey(GUIBoundKeyEventArgs args)
    {
        var dir = KeyDirection(args.Function);

        if (dir == Direction.None)
            return;

        args.Handle();
        if (!_held.Contains(dir))
            _held.Insert(0, dir);
    }

    private void OnKeyRelease(GUIBoundKeyEventArgs args)
    {
        var dir = KeyDirection(args.Function);
        if (dir != Direction.None)
            _held.Remove(dir);
    }

    private static Direction KeyDirection(BoundKeyFunction function)
    {
        if (function == EngineKeyFunctions.MoveUp || function == EngineKeyFunctions.TextCursorUp)
            return Direction.Up;
        if (function == EngineKeyFunctions.MoveDown || function == EngineKeyFunctions.TextCursorDown)
            return Direction.Down;
        if (function == EngineKeyFunctions.MoveLeft || function == EngineKeyFunctions.TextCursorLeft)
            return Direction.Left;
        if (function == EngineKeyFunctions.MoveRight || function == EngineKeyFunctions.TextCursorRight)
            return Direction.Right;
        return Direction.None;
    }

    private Direction DesiredDir => _held.Count > 0 ? _held[0] : Direction.None;

    private bool HasInputControl =>
        UserInterfaceManager.KeyboardFocused == this ||
        (UserInterfaceManager.KeyboardFocused == null && UserInterfaceManager.CurrentlyHovered == this);

    protected override void Draw(DrawingHandleScreen handle)
    {
        var now = _timing.RealTime;
        var dt = (float) (now - _lastFrame).TotalSeconds;
        _lastFrame = now;

        if (_running && !_finished)
            Tick(Math.Min(dt, 0.1f));

        DrawField(handle, (float) now.TotalSeconds);
    }

    private void Tick(float dt)
    {
        if (!HasInputControl)
        {
            _stepAccum = 0f;
            return;
        }

        if (_invuln > 0f)
            _invuln -= dt;

        var stepInterval = _fastMode ? 0.085f : 0.15f;
        _stepAccum += dt;
        while (_stepAccum >= stepInterval)
        {
            _stepAccum -= stepInterval;
            StepPlayer();
            if (!_running)
                return;
        }

        foreach (var necro in _necrosis)
        {
            MoveNecrosis(necro, dt);
            if (CheckNecrosisHit(necro))
                return;
        }

        foreach (var glitch in _glitches)
        {
            MoveGlitch(glitch, dt);
            if (CheckGlitchHit(glitch))
                return;
        }
    }

    #region Игрок

    private void StepPlayer()
    {
        var desired = DesiredDir;
        if (desired != Direction.None)
            _dir = desired;
        else if (!_drawing)
        {
            Settle();
            return;
        }

        if (_dir == Direction.None)
            return;

        var (dx, dy) = Delta(_dir);
        var nx = _player.X + dx;
        var ny = _player.Y + dy;

        if (nx < 0 || ny < 0 || nx >= FieldW || ny >= FieldH)
        {
            Settle();
            return;
        }

        var target = _cells[nx, ny];

        if (target == Cell.Filled)
        {
            if (_drawing)
                CloseCapture();
            MoveTo(nx, ny);
            return;
        }

        if (!_drawing)
        {
            _drawing = true;
            _trail.Clear();
            _trail.Add(_player);
        }

        if (target == Cell.Trail)
        {
            if (_trail.Count >= 2 && _trail[^2].X == nx && _trail[^2].Y == ny)
            {
                var last = _trail[^1];
                _cells[last.X, last.Y] = Cell.Empty;
                _trail.RemoveAt(_trail.Count - 1);
                MoveTo(nx, ny);

                if (_trail.Count <= 1)
                {
                    _trail.Clear();
                    _drawing = false;
                }
            }
            else
            {
                Settle();
            }
            return;
        }

        _cells[nx, ny] = Cell.Trail;
        _trail.Add((nx, ny));
        MoveTo(nx, ny);
    }

    /// <summary>
    /// Фиксирует отрисовку на текущей клетке, если шаг невозможен.
    /// </summary>
    private void Settle()
    {
        _playerFrom = _player;
    }

    private void MoveTo(int x, int y)
    {
        _playerFrom = _player;
        _player = (x, y);
    }

    private void CloseCapture()
    {
        foreach (var (x, y) in _trail)
            _cells[x, y] = Cell.Filled;
        _trail.Clear();
        _drawing = false;
        _playerFrom = _player;

        FloodFillCapture();

        _necrosis.RemoveAll(n => _cells[(int) n.X, (int) n.Y] != Cell.Empty);

        if (_necrosis.Count < MaxNecrosis && _random.Next(3) != 0)
            SpawnNecrosis(1);

        if (_glitches.Count < MaxGlitches)
        {
            var spawn = FindEdgeCell();
            if (spawn != null)
                _glitches.Add(new Glitch { X = spawn.Value.X, Y = spawn.Value.Y, Dir = Direction.Right, NextStep = 0 });
        }

        RecomputeCoverage();
        _audio.PlayGlobal("/Audio/Effects/Arcade/player_charge.ogg", Filter.Local(), false, AudioParams.Default.WithVolume(-6f));
        EmitStats();

        if (_coverage >= TargetCoverage)
            Win();
    }

    private void FloodFillCapture()
    {
        var reach = new bool[FieldW, FieldH];
        var queue = new Queue<(int X, int Y)>();

        foreach (var n in _necrosis)
        {
            var cx = (int) Math.Clamp(n.X, 0, FieldW - 1);
            var cy = (int) Math.Clamp(n.Y, 0, FieldH - 1);
            if (_cells[cx, cy] == Cell.Empty && !reach[cx, cy])
            {
                reach[cx, cy] = true;
                queue.Enqueue((cx, cy));
            }
        }

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            Spread(reach, queue, x + 1, y);
            Spread(reach, queue, x - 1, y);
            Spread(reach, queue, x, y + 1);
            Spread(reach, queue, x, y - 1);
        }

        for (var x = 1; x < FieldW - 1; x++)
        {
            for (var y = 1; y < FieldH - 1; y++)
            {
                if (_cells[x, y] == Cell.Empty && !reach[x, y])
                    _cells[x, y] = Cell.Filled;
            }
        }
    }

    private void Spread(bool[,] reach, Queue<(int X, int Y)> queue, int x, int y)
    {
        if (x < 0 || y < 0 || x >= FieldW || y >= FieldH)
            return;
        if (_cells[x, y] != Cell.Empty || reach[x, y])
            return;
        reach[x, y] = true;
        queue.Enqueue((x, y));
    }

    private void Win()
    {
        _running = false;
        _finished = true;
        _won = true;

        var baseQuality = _fastMode ? 40 : 70;
        var coverageBonus = (float) ((_coverage - TargetCoverage) * (_fastMode ? 100f : 120f));
        var quality = (int) Math.Clamp(baseQuality + coverageBonus + _integrity * 0.1f, 0, _fastMode ? 65 : 100);

        _audio.PlayGlobal("/Audio/Effects/Arcade/newgame.ogg", Filter.Local(), false, AudioParams.Default.WithVolume(-4f));
        OnFinished?.Invoke(true, quality);
    }

    #endregion

    #region Враги

    private void SpawnNecrosis(int count)
    {
        for (var i = 0; i < count && _necrosis.Count < MaxNecrosis; i++)
        {
            var tries = 0;
            while (tries++ < 100)
            {
                var x = _random.Next(2, FieldW - 2);
                var y = _random.Next(2, FieldH - 2);
                if (_cells[x, y] != Cell.Empty)
                    continue;

                var nx = _player.X - x;
                var ny = _player.Y - y;
                if (nx * nx + ny * ny < 100)
                    continue;

                var angle = (float) (_random.NextDouble() * Math.PI * 2);
                _necrosis.Add(new Necrosis
                {
                    X = x + 0.5f,
                    Y = y + 0.5f,
                    Speed = 2.6f + (float) _random.NextDouble() * 1.8f,
                    Phase = (float) _random.NextDouble() * MathF.Tau,
                    VX = MathF.Sin(angle),
                    VY = MathF.Cos(angle),
                });
                break;
            }
        }
    }

    private void MoveNecrosis(Necrosis n, float dt)
    {
        var r = 0.34f;

        var newX = n.X + n.VX * n.Speed * dt;
        if (CircleHitsFilled(newX, n.Y, r))
            n.VX = -n.VX;
        else
            n.X = newX;

        var newY = n.Y + n.VY * n.Speed * dt;
        if (CircleHitsFilled(n.X, newY, r))
            n.VY = -n.VY;
        else
            n.Y = newY;
    }

    private bool CircleHitsFilled(float x, float y, float r)
    {
        var minX = (int) MathF.Floor(x - r);
        var maxX = (int) MathF.Floor(x + r);
        var minY = (int) MathF.Floor(y - r);
        var maxY = (int) MathF.Floor(y + r);

        for (var cx = minX; cx <= maxX; cx++)
        {
            for (var cy = minY; cy <= maxY; cy++)
            {
                if (cx < 0 || cy < 0 || cx >= FieldW || cy >= FieldH)
                    continue;
                if (_cells[cx, cy] != Cell.Empty)
                    return true;
            }
        }

        return false;
    }

    private bool CheckNecrosisHit(Necrosis n)
    {
        var pr = 0.3f;
        var dx = n.X - (_player.X + 0.5f);
        var dy = n.Y - (_player.Y + 0.5f);

        if (dx * dx + dy * dy < (pr + 0.34f) * (pr + 0.34f))
        {
            LoseTrail(HitDamage().PlayerHit, false);
            return true;
        }

        foreach (var (tx, ty) in _trail)
        {
            var cx = Math.Clamp(n.X, tx, tx + 1);
            var cy = Math.Clamp(n.Y, ty, ty + 1);
            var ex = n.X - cx;
            var ey = n.Y - cy;
            if (ex * ex + ey * ey < 0.34f * 0.34f)
            {
                LoseTrail(HitDamage().TrailHit, false);
                return true;
            }
        }

        return false;
    }

    private void MoveGlitch(Glitch g, double dt)
    {
        g.NextStep -= dt;
        while (g.NextStep <= 0)
        {
            g.NextStep += 0.22f;

            var options = new List<Direction>();
            foreach (var dir in new[] { g.Dir, TurnLeft(g.Dir), TurnRight(g.Dir), TurnLeft(TurnLeft(g.Dir)) })
            {
                var (dx, dy) = Delta(dir);
                var nx = g.X + dx;
                var ny = g.Y + dy;
                if (nx < 0 || ny < 0 || nx >= FieldW || ny >= FieldH)
                    continue;
                if (_cells[nx, ny] != Cell.Filled || !IsEdgeCell(nx, ny))
                    continue;
                options.Add(dir);
            }

            if (options.Count == 0)
            {
                var spawn = FindEdgeCell();
                if (spawn != null)
                {
                    g.X = spawn.Value.X;
                    g.Y = spawn.Value.Y;
                    g.Dir = Direction.Right;
                }
                return;
            }

            var chosen = options.Contains(g.Dir) ? g.Dir : options[_random.Next(options.Count)];
            g.Dir = chosen;
            var (mx, my) = Delta(chosen);
            g.X += mx;
            g.Y += my;
        }
    }

    private bool CheckGlitchHit(Glitch g)
    {
        if (_invuln > 0f)
            return false;

        var dx = Math.Abs(g.X + 0.5f - (_player.X + 0.5f));
        var dy = Math.Abs(g.Y + 0.5f - (_player.Y + 0.5f));
        if (dx > 0.7f || dy > 0.7f)
            return false;

        LoseTrail(HitDamage().PlayerHit + 6f, true);
        _invuln = 2f;
        return true;
    }

    private void LoseTrail(float damage, bool fullRespawn)
    {
        _integrity = Math.Max(0f, _integrity - damage);

        var back = fullRespawn || _trail.Count == 0 ? _respawn : _trail[0];

        foreach (var (x, y) in _trail)
            _cells[x, y] = Cell.Empty;
        _trail.Clear();
        _drawing = false;
        _dir = Direction.None;

        _playerFrom = back;
        _player = back;
        _invuln = Math.Max(_invuln, 1f);

        _audio.PlayGlobal("/Audio/Effects/bite.ogg", Filter.Local(), false, AudioParams.Default.WithVolume(-8f));
        EmitStats();

        if (_integrity <= 0f)
            Fail();
    }

    private void Fail()
    {
        _running = false;
        _finished = true;
        _won = false;

        _audio.PlayGlobal("/Audio/Effects/Arcade/gameover.ogg", Filter.Local(), false, AudioParams.Default.WithVolume(-4f));
        OnFinished?.Invoke(false, 0);
    }

    #endregion

    private (float TrailHit, float PlayerHit) HitDamage() => _fastMode ? FastDamage : FineDamage;

    private bool IsEdgeCell(int x, int y)
    {
        return _cells[x, y] == Cell.Filled &&
               (x == 0 || y == 0 || x == FieldW - 1 || y == FieldH - 1 ||
                _cells[x + 1, y] != Cell.Filled || _cells[x - 1, y] != Cell.Filled ||
                _cells[x, y + 1] != Cell.Filled || _cells[x, y - 1] != Cell.Filled);
    }

    private (int X, int Y)? FindEdgeCell()
    {
        for (var i = 0; i < 200; i++)
        {
            var x = _random.Next(1, FieldW - 1);
            var y = _random.Next(1, FieldH - 1);
            if (IsEdgeCell(x, y) && Math.Abs(x - _player.X) + Math.Abs(y - _player.Y) > 10)
                return (x, y);
        }

        return null;
    }

    private void RecomputeCoverage()
    {
        _silhouetteFilled = 0;
        for (var x = 1; x < FieldW - 1; x++)
        {
            for (var y = 1; y < FieldH - 1; y++)
            {
                if (_silhouette[x, y] && _cells[x, y] == Cell.Filled)
                    _silhouetteFilled++;
            }
        }

        _coverage = _silhouetteTotal > 0 ? (float) _silhouetteFilled / _silhouetteTotal : 0f;
    }

    private void EmitStats()
    {
        OnStatsChanged?.Invoke(Coverage, (int) _integrity);
    }

    private static (int DX, int DY) Delta(Direction dir) => dir switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => (0, 0),
    };

    private static Direction TurnLeft(Direction dir) => dir switch
    {
        Direction.Up => Direction.Left,
        Direction.Down => Direction.Right,
        Direction.Left => Direction.Down,
        Direction.Right => Direction.Up,
        _ => Direction.None,
    };

    private static Direction TurnRight(Direction dir) => dir switch
    {
        Direction.Up => Direction.Right,
        Direction.Down => Direction.Left,
        Direction.Left => Direction.Up,
        Direction.Right => Direction.Down,
        _ => Direction.None,
    };

    #region Отрисовка

    private void DrawField(DrawingHandleScreen handle, float time)
    {
        var origin = new Vector2((PixelSize.X - FieldW * CellPx) / 2f, (PixelSize.Y - FieldH * CellPx) / 2f);

        handle.DrawRect(new UIBox2(origin, origin + new Vector2(FieldW * CellPx, FieldH * CellPx)), Color.FromHex("#0b1f10"));

        for (var gx = 0; gx <= FieldW; gx++)
        {
            handle.DrawLine(origin + new Vector2(gx * CellPx, 0), origin + new Vector2(gx * CellPx, FieldH * CellPx),
                Color.FromHex("#14361c"));
        }

        for (var gy = 0; gy <= FieldH; gy++)
        {
            handle.DrawLine(origin + new Vector2(0, gy * CellPx), origin + new Vector2(FieldW * CellPx, gy * CellPx),
                Color.FromHex("#14361c"));
        }

        for (var x = 0; x < FieldW; x++)
        {
            for (var y = 0; y < FieldH; y++)
            {
                var rect = new UIBox2(origin + new Vector2(x * CellPx, y * CellPx),
                    origin + new Vector2((x + 1) * CellPx, (y + 1) * CellPx));
                var cell = _cells[x, y];

                if (cell == Cell.Filled)
                {
                    var shade = (x * 7 + y * 13) % 3;
                    var color = shade switch
                    {
                        0 => Color.FromHex("#7e4040"),
                        1 => Color.FromHex("#743a3a"),
                        _ => Color.FromHex("#874747"),
                    };
                    handle.DrawRect(rect, color);

                    if (x == 0 || y == 0 || x == FieldW - 1 || y == FieldH - 1 ||
                        _cells[x + 1, y] == Cell.Empty || _cells[x - 1, y] == Cell.Empty ||
                        _cells[x, y + 1] == Cell.Empty || _cells[x, y - 1] == Cell.Empty)
                        handle.DrawRect(rect, Color.FromHex("#a85b5b").WithAlpha(0.55f));

                    continue;
                }

                if (cell == Cell.Trail)
                {
                    handle.DrawRect(rect, Color.FromHex("#d8ff5f").WithAlpha(0.75f));
                    handle.DrawRect(new UIBox2(rect.Left + 3, rect.Top + 3, rect.Right - 3, rect.Bottom - 3),
                        Color.FromHex("#f4ffd9"));
                    continue;
                }

                if (_silhouette[x, y])
                {
                    handle.DrawRect(rect, Color.FromHex("#3ed26b").WithAlpha(0.07f));
                    handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Right, rect.Top + 1),
                        Color.FromHex("#3ed26b").WithAlpha(0.4f));
                    handle.DrawRect(new UIBox2(rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom),
                        Color.FromHex("#3ed26b").WithAlpha(0.4f));
                    handle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Left + 1, rect.Bottom),
                        Color.FromHex("#3ed26b").WithAlpha(0.4f));
                    handle.DrawRect(new UIBox2(rect.Right - 1, rect.Top, rect.Right, rect.Bottom),
                        Color.FromHex("#3ed26b").WithAlpha(0.4f));
                }
            }
        }

        if (_trail.Count > 1)
        {
            for (var i = 1; i < _trail.Count; i++)
            {
                var a = origin + new Vector2(_trail[i - 1].X + 0.5f, _trail[i - 1].Y + 0.5f) * CellPx;
                var b = origin + new Vector2(_trail[i].X + 0.5f, _trail[i].Y + 0.5f) * CellPx;
                handle.DrawLine(a, b, Color.FromHex("#d8ff5f"));
            }
        }

        foreach (var n in _necrosis)
        {
            var pos = origin + new Vector2(n.X, n.Y) * CellPx;
            var pulse = 3.4f + MathF.Sin(time * 8f + n.Phase) * 0.9f;
            handle.DrawCircle(pos, pulse + 2.5f, Color.FromHex("#ff3b3b").WithAlpha(0.25f));
            handle.DrawCircle(pos, pulse, Color.FromHex("#ff3b3b"));
            handle.DrawCircle(pos, pulse * 0.45f, Color.FromHex("#7a0f0f"));
        }

        foreach (var g in _glitches)
        {
            var pos = origin + new Vector2(g.X + 0.5f, g.Y + 0.5f) * CellPx;
            handle.DrawRect(new UIBox2(pos - new Vector2(4, 4), pos + new Vector2(4, 4)), Color.FromHex("#3fa7ff"));
            handle.DrawRect(new UIBox2(pos - new Vector2(1.5f, 1.5f), pos + new Vector2(1.5f, 1.5f)),
                Color.FromHex("#cfebff"));
        }

        var stepInterval = _fastMode ? 0.085f : 0.15f;
        var progress = _running ? Math.Clamp(_stepAccum / stepInterval, 0f, 1f) : 1f;
        var lerped = new Vector2(
            MathHelper.Lerp(_playerFrom.X + 0.5f, _player.X + 0.5f, progress),
            MathHelper.Lerp(_playerFrom.Y + 0.5f, _player.Y + 0.5f, progress));
        var playerPos = origin + lerped * CellPx;

        var glow = _fastMode ? Color.FromHex("#ffb13b") : Color.FromHex("#4fe3ff");
        var blink = _invuln > 0f && MathF.Sin(time * 20f) > 0f;
        if (!blink)
        {
            handle.DrawCircle(playerPos, 7f, glow.WithAlpha(0.3f));
            handle.DrawCircle(playerPos, 4.5f, glow);
            handle.DrawCircle(playerPos, 2f, Color.White);
        }

        if (_running && !_finished && !HasInputControl)
        {
            handle.DrawRect(new UIBox2(origin, origin + new Vector2(FieldW * CellPx, FieldH * CellPx)),
                Color.Black.WithAlpha(0.55f));
        }
    }

    #endregion
}
