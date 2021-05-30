using RavenBot.Twitch;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RavenBot.Forms
{
    public partial class TokenGeneratorForm : Form
    {
        private readonly SettingsConfigurationForm settingsForm;
        private readonly TwitchOAuthTokenGenerator generator = new TwitchOAuthTokenGenerator();
        private Process browserProcess;

        public TokenGeneratorForm(SettingsConfigurationForm settingsForm)
        {
            generator.AccessTokenReceived += Generator_AccessTokenReceived;
            InitializeComponent();
            this.settingsForm = settingsForm;
        }

        private void Generator_AccessTokenReceived(object sender, TwitchOAuthResult e)
        {
            settingsForm.OnAccessTokenReceived(e);

            try
            {
                if (browserProcess != null && !browserProcess.HasExited)
                {
                    browserProcess.Close();
                }
            }
            catch { }

            this.Close();
        }

        private async void TokenGeneratorForm_Load(object sender, EventArgs e)
        {
            await generator.StartAuthenticationProcess(url =>
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c start http://127.0.0.1:8182/twitchredirect"
                };
                browserProcess = Process.Start(psi);
            }, () => RestartAsAdmin());
        }

        static void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo("RavenBot.exe") { Verb = "runas" };
            Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}
