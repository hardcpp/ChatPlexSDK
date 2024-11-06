using System;

namespace CP_SDK.Logging
{
    /// <summary>
    /// Console logger implementation
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Log implementation
        /// </summary>
        /// <param name="p_Type">Kind</param>
        /// <param name="p_Data">Data</param>
        protected override void LogImplementation(ELogType p_Type, string p_Data)
        {
            switch (p_Type)
            {
                case ELogType.Error:    Console.WriteLine($"[ERROR] {p_Data}");     break;
                case ELogType.Warning:  Console.WriteLine($"[WARNING] {p_Data}");   break;
                case ELogType.Info:     Console.WriteLine($"[INFO] {p_Data}");      break;
                case ELogType.Debug:    Console.WriteLine($"[DEBUG] {p_Data}");     break;
            }
        }
        /// <summary>
        /// Log implementation
        /// </summary>
        /// <param name="p_Type">Kind</param>
        /// <param name="p_Data">Data</param>
        protected override void LogImplementation(ELogType p_Type, Exception p_Data)
        {
            switch (p_Type)
            {
                case ELogType.Error:    Console.WriteLine($"[ERROR] {p_Data}");     break;
                case ELogType.Warning:  Console.WriteLine($"[WARNING] {p_Data}");   break;
                case ELogType.Info:     Console.WriteLine($"[INFO] {p_Data}");      break;
                case ELogType.Debug:    Console.WriteLine($"[DEBUG] {p_Data}");     break;
            }
        }
    }
}