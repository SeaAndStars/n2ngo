// 那些梦想就应该作为成长的代价而被遗忘在风里吗？ - LGF

using N2NGO.UtilsClass;
using N2NGO.Views;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace N2NGO
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
#if DEBUG
        bool _debugConsoleAlloc = false;
#endif
        public App()
        {
#if DEBUG
#else
            TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(TaskScheduler_UnobservedTaskException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Application_DispatcherUnhandledException);
#endif

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
#if DEBUG
            [DllImport("kernel32.dll")]
            static extern bool AllocConsole();

            if (AllocConsole())
            {
                _debugConsoleAlloc = true;

                Console.Title = $"N2N GO({SharedData.VersionString}) Debug Console";
            }
            else MessageBox.Show("Cannot alloc a new console", "N2N GO Debug");
#endif
        }


        ~App()
        {
#if DEBUG
            if (_debugConsoleAlloc)
            {
                [DllImport("kernel32.dll")]
                static extern bool FreeConsole();

                FreeConsole();
            }
#endif
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            var content = string.Format("N2N GO 发生了未处理的异常\n" +
                "[Exception]\n" +
                "  HC {0}\n" +
                "  Message {{{1}}}\n" +
                "  HResult {2}\n" +
                "  StackTrace {{{3}}}\n" +
                "\n" +
                "Ignore? | 要忽略并继续吗？"
                ,
                e.Exception.GetHashCode(),
                e.Exception.Message,
                e.Exception.HResult,
                e.Exception.StackTrace
                );

            if (MessageBox.Show(content, "N2N GO - Unobserved Task Exception | N2N GO 错误", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                e.SetObserved();
                return;
            }
            else
            {
                return;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception == null)
            {
                App.Current.Shutdown(2);
                return;
            }

            var content = string.Format("N2N GO 发生了未处理的异常\n" +
                "[Exception]\n" +
                "  HC {0}\n" +
                "  Message {{{1}}}\n" +
                "  HResult {2}\n" +
                "  StackTrace {{{3}}}\n" +
                "\n" +
                "{4}"
                ,

                exception.GetHashCode(),
                exception.Message,
                exception.HResult,
                exception.StackTrace,
                (e.IsTerminating ? "Terminating | N2N GO 正在终止" : "Ignore? | 要忽略并继续吗？")
                );

            if (!e.IsTerminating)
            {
                if (MessageBox.Show(content, "N2N GO - Unhandled Exception | N2N GO 错误", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    return;
                }
                else
                {
                    App.Current.Shutdown(1);
                    return;
                }
            }
        }

        private static void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var content = string.Format("N2N GO 发生了未处理的异常\n" +
                "[Dispatcher]\n" +
                "  HC {0}\n" +
                "  Thread {1}\n" +
                "\n" +
                "[Exception]\n" +
                "  HC {2}\n" +
                "  Message {{{3}}}\n" +
                "  HResult {4}\n" +
                "  StackTrace {{{5}}}\n" +
                "\n" +
                "Ignore? | 要忽略并继续吗？"
                ,
                e.Dispatcher.GetHashCode(),
                e.Dispatcher.Thread.Name,

                e.Exception.GetHashCode(),
                e.Exception.Message,
                e.Exception.HResult,
                e.Exception.StackTrace
                );

            if (MessageBox.Show(content, "N2N GO - Unhandled WPF Exception | N2N GO 错误", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                e.Handled = true;
                return;
            }
            else
            {
                e.Handled = false;
                App.Current.Shutdown(1);
                return;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Timeline.DesiredFrameRateProperty.OverrideMetadata(
               typeof(Timeline),
               new FrameworkPropertyMetadata { DefaultValue = null }
               );
        }
    }
}

