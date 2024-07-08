using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Core.WordMarquee
{
    /// <summary>
    /// Singleton that routes calls to WordMarqueeWindow (in its own thread), takes care of hiding the
    /// window after inactivity
    /// </summary>
    public class WordMarqueeManager2
    {
        private static readonly Lazy<WordMarqueeManager2> _instance = new Lazy<WordMarqueeManager2>(() => new WordMarqueeManager2());

        private readonly object _lock = new object();

        private readonly List<Lane> _lanes = new List<Lane>();      // this is needed when a window gets closed and a new one is create later
        private readonly Queue<Lane> _pending_lanes = new Queue<Lane>();
        private readonly Queue<Word> _pending_words = new Queue<Word>();

        private readonly System.Timers.Timer _wakeup_timer;     // used if calls are coming in before the window is ready (values build up in pending queues)
        private readonly System.Timers.Timer _inactivity_timer;     // fires after not receiving words for too long, closes the window (just minimizing for now)

        private readonly Thread _ui_thread = null;      // the thread that the window runs on
        private WordMarqueeWindow _window = null;       // will be null during periods of inactivity (just minimizing for now)

        private WordMarqueeManager2()
        {
            _ui_thread = new Thread(CreateNewWindow);
            _ui_thread.SetApartmentState(ApartmentState.STA);
            _ui_thread.IsBackground = true;
            _ui_thread.Start();

            _wakeup_timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = false,
                Interval = 150,
            };

            _wakeup_timer.Elapsed += WakeUp_Elapsed;

            _inactivity_timer = new System.Timers.Timer()
            {
                AutoReset = false,
                Enabled = false,
                Interval = TimeSpan.FromMinutes(5).TotalMilliseconds,
            };

            _inactivity_timer.Elapsed += Inactivity_Elapsed;
        }

        // Must have at least one lane before adding words
        public static void AddLane(Lane lane)
        {
            var instance = _instance.Value;

            lock (instance._lock)
            {
                instance._lanes.Add(lane);

                if (instance._window == null)
                {
                    instance._pending_lanes.Enqueue(lane);
                    instance._wakeup_timer.Start();
                }
                else
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
                if (instance._window == null)
                {
                    instance._pending_words.Enqueue(word);
                    instance._wakeup_timer.Start();
                }
                else
                {
                    instance._inactivity_timer.Stop();
                    instance._inactivity_timer.Start();

                    var dispatcher = Dispatcher.FromThread(instance._ui_thread);
                    dispatcher.Invoke(() =>
                    {
                        instance._window.WindowState = WindowState.Normal;
                        instance._window.AddWord(word);
                    });
                }
            }
        }

        private void WakeUp_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (_window == null)
                    return;     // it hasn't been created yet

                _wakeup_timer.Stop();

                var dispatcher = Dispatcher.FromThread(_ui_thread);
                dispatcher.Invoke(() =>
                {
                    while(_pending_lanes.Count > 0)
                        _window.AddLane(_pending_lanes.Dequeue());

                    _window.WindowState = WindowState.Normal;

                    while(_pending_words.Count > 0)
                        _window.AddWord(_pending_words.Dequeue());
                });
            }
        }
        private void Inactivity_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lock)
            {
                _inactivity_timer.Stop();       // shouldn't be necessary since AutoReset is false

                if (_window == null)
                    return;     // it hasn't been created yet, and it's minimized on creation (if window is still null when this timer fires, there's a larger problem)

                var dispatcher = Dispatcher.FromThread(_ui_thread);

                // TODO: instead of minimizing, test closing the window, make sure the thread stays alive

                dispatcher.Invoke(() => _window.WindowState = WindowState.Minimized);
            }
        }

        private void CreateNewWindow()
        {
            int threadID = Thread.CurrentThread.ManagedThreadId;

            lock (_lock)
            {
                _window = new WordMarqueeWindow()
                {
                    Title = $"Thread ID: {threadID}",
                    WindowState = WindowState.Minimized,
                };
                _window.Show();
            }

            Dispatcher.Run();
        }
    }
}
