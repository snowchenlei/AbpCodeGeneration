using AbpCodeGeneration.VisualStudio.Shared;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AbpCodeGeneration.VisualStudio.Services
{
    [Export(typeof(IShellService))]
    public class ShellService : IShellService
    {
        [ImportingConstructor]
        public ShellService()
        {
        }

        public void OpenUrl(string uri)
        {
            System.Diagnostics.Process.Start(uri);
        }

        public void ShowDialog(string title, Control content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            //var uri = new Uri(@"pack://application:,,,/Gitee.UI;component/Resources/Images/logo.png");
            //var icon = new BitmapImage(uri);
            var win = new DialogWindow
            {
                Content = content,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Title = title,
            };

            //content.Closed += () =>
            //{
            //    win.Close();
            //};
            win.ShowModal();
        }
    }
}