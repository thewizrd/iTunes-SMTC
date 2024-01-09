namespace iTunes.SMTC.Utils
{
    public static class DelegateUtils
    {
        public static bool HasListeners(this Delegate @delegate)
        {
            return @delegate?.GetInvocationList()?.Length > 0;
        }
    }
}
