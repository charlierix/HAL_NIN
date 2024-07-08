using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Core.WordMarquee
{
    public class WordMarqueeManager
    {
        private static readonly Lazy<WordMarqueeManager> _instance = new Lazy<WordMarqueeManager>(() => new WordMarqueeManager());

        private readonly object _lock;
        private readonly Thread _ui_thread;
        //private readonly DispatcherTimer _inactivity_timer;
        private readonly System.Timers.Timer _inactivity_timer;
        private WordMarqueeWindow _window = null;

        private WordMarqueeManager()
        {
            _lock = new object();

            _ui_thread = new Thread(() => Dispatcher.Run());
            _ui_thread.SetApartmentState(ApartmentState.STA);
            _ui_thread.IsBackground = true;
            _ui_thread.Start();

            _inactivity_timer = new System.Timers.Timer()
            {
                AutoReset = false,
                Enabled = false,
                Interval = TimeSpan.FromMinutes(5).TotalMilliseconds,
            };

            _inactivity_timer.Elapsed += Inactivity_Elapsed;
        }

        public static void DefineLane(Lane lane)
        {

        }

        public static void AddWord(Word word)
        {

        }

        private void Inactivity_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock( _lock)
            {
                _inactivity_timer.Stop();       // shouldn't be necessary since AutoReset is false

                //c.Invoke(new setTextCallBack(SetText), new object[] { c, txt });
                //_ui_thread.


            }
        }
    }
}
