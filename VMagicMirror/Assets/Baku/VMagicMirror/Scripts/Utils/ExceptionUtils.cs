using System;

namespace Baku.VMagicMirror
{
    public static class ExceptionUtils
    {
        public static void TryWithoutException(Action act)
        {
            try
            {
                act();
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        public static T TryWithoutException<T>(Func<T> func, Func<T> funcForDefault)
        {
            try
            {
                return func();
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return funcForDefault();
            }
        }

        public static T TryWithoutException<T>(Func<T> func)
        {
            return TryWithoutException(func, () => default);
        }
    }
}

