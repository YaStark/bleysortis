using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bleysortis.Main
{
    public class Game : IDisposable
    {
        private readonly Timer _timer;
        private Map _map = new Map();
        private bool _busy;
        private DateTime _lastTick;

        public Game()
        {
            _timer = new Timer(OnTick, null, 0, Timeout.Infinite);
        }

        public IEnumerable<BaseObject> EnumerateObjects()
        {
            return _map.EnumerateItems();
        }

        public void OnStart()
        {
            _lastTick = DateTime.Now;
            _timer.Change(0, 20);
        }

        public void OnTick(object param)
        {
            if(_busy)
            {
                return;
            }

            try
            {
                var tickTime = DateTime.Now;
                var delay = (int)(tickTime - _lastTick).TotalMilliseconds;
                _lastTick = tickTime;
                if (delay < 0) delay = 1;
                foreach (var item in EnumerateObjects())
                {
                    item.OnTick(delay);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
            }
            finally
            {
                _busy = false;
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
