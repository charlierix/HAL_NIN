using System.Windows.Threading;

namespace Core.WordMarquee
{
    /// <summary>
    /// Singleton that routes calls to WordMarqueeWindow (in its own thread), takes care of closing the
    /// window after inactivity
    /// </summary>
    public class WordMarqueeManager
    {
        const double INACTIVITY_MINUTES = 3;

        private static readonly Lazy<WordMarqueeManager> _instance = new(() => new WordMarqueeManager());

        private readonly object _lock = new();

        private readonly Dictionary<string, Lane> _lanes = [];      // this is needed when a window gets closed and a new one is create later
        private readonly Queue<Word> _pending_words = new();

        private readonly System.Timers.Timer _show_timer;     // used to create a new window when words arrive while there is no window showing
        private readonly System.Timers.Timer _inactivity_timer;     // fires after not receiving words for too long, closes the window (just minimizing for now)

        private readonly Thread _ui_thread = null;      // the thread that the window runs on
        private WordMarqueeWindow _window = null;       // will be null during periods of inactivity

        private WordMarqueeManager()
        {
            _ui_thread = new Thread(Dispatcher.Run);
            _ui_thread.SetApartmentState(ApartmentState.STA);
            _ui_thread.IsBackground = true;
            _ui_thread.Start();

            _show_timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = false,
                Interval = 150,
            };

            _show_timer.Elapsed += Show_Elapsed;

            _inactivity_timer = new System.Timers.Timer()
            {
                AutoReset = false,
                Enabled = false,
                Interval = TimeSpan.FromMinutes(INACTIVITY_MINUTES).TotalMilliseconds,
            };

            _inactivity_timer.Elapsed += Inactivity_Elapsed;
        }

        // Must have at least one lane before adding words
        public static void AddLane(Lane lane)
        {
            var instance = _instance.Value;

            lock (instance._lock)
            {
                if (instance._lanes.TryAdd(lane.Name, lane) && instance._window != null)
                {
                    var dispatcher = Dispatcher.FromThread(instance._ui_thread);
                    dispatcher.Invoke(() => instance._window.AddLane(lane));
                }
            }
        }
        public static void AddWord(Word word)
        {
            var instance = _instance.Value;

            lock (instance._lock)
            {
                instance._inactivity_timer.Stop();
                instance._inactivity_timer.Start();

                if (instance._window == null)
                {
                    instance._pending_words.Enqueue(word);
                    instance._show_timer.Start();
                }
                else
                {
                    var dispatcher = Dispatcher.FromThread(instance._ui_thread);
                    dispatcher.Invoke(() =>
                    {
                        instance._window.AddWord(word);
                    });
                }
            }
        }

        private void Show_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lock)
            {
                _show_timer.Stop();

                var dispatcher = Dispatcher.FromThread(_ui_thread);

                dispatcher.Invoke(() =>
                {
                    int threadID = Environment.CurrentManagedThreadId;

                    // TODO: figure out if the window should position itself or if this should

                    _window = new WordMarqueeWindow()
                    {
                        Title = $"Thread ID: {threadID}",
                        //WindowStartupLocation = WindowStartupLocation.Manual,
                    };
                    _window.Show();

                    foreach (var lane in _lanes.Values)
                        _window.AddLane(lane);

                    while (_pending_words.Count > 0)
                        _window.AddWord(_pending_words.Dequeue());
                });
            }
        }
        private void Inactivity_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (_window == null)
                {
                    // It hasn't been created yet.  Should never happen, this timer tick should be much longer than _show_timer
                    // Set the timer to keep firing in case the window shows later
                    _inactivity_timer.AutoReset = true;
                    _inactivity_timer.Start();
                    return;
                }

                _inactivity_timer.Stop();
                _inactivity_timer.AutoReset = false;

                var dispatcher = Dispatcher.FromThread(_ui_thread);

                dispatcher.Invoke(() =>
                {
                    _window.Close();
                    _window = null;
                });
            }
        }
    }
}
