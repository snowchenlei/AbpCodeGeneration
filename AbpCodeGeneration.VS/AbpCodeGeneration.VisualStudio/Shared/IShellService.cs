using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AbpCodeGeneration.VisualStudio.Shared
{
    public interface IShellService
    {
        /// <summary>
        /// Opens the user's default browser to the specified URL.
        /// </summary>
        /// <param name="url">The absolute URI to open</param>
        void OpenUrl(string uri);

        void ShowDialog(string title, Control content);
    }
}