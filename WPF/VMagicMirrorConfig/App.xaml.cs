using System;
using System.Windows;
using System.Windows.Threading;

namespace Baku.VMagicMirrorConfig
{
    public partial class App : Application
    {
        public event Action? MainWindowInitialized;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnUnhandledExceptionHappened;
            ModelInstaller.Initialize();
            ModelResolver.Instance.Resolve<PreferenceFileManager>().Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DispatcherUnhandledException -= OnUnhandledExceptionHappened;
            base.OnExit(e);
        }

        private void OnUnhandledExceptionHappened(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //マジの異常系でしかココに来ないのならばダイアログを出したいのだが、
            //TaskCanceledとかと混同する心配がないようにコード直すのが先か
            LogOutput.Instance.Write(e.Exception);
        }

        public void RaiseWindowInitialized() => MainWindowInitialized?.Invoke();
    }
}
