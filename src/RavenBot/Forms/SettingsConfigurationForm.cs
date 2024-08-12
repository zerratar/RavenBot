using RavenBot.Core;
using RavenBot.Twitch;
using System;
using System.Windows.Forms;

namespace RavenBot.Forms
{
    public partial class SettingsConfigurationForm : Form
    {
        private TwitchOAuthResult authResult;
        private readonly Core.IAppSettings existingAppSettings;

        public SettingsConfigurationForm(Core.IAppSettings existingAppSettings)
        {
            InitializeComponent();
            this.existingAppSettings = existingAppSettings;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var settings = new Core.AppSettings(authResult?.User ?? inputChannel.Text, inputToken.Text, inputChannel.Text, existingAppSettings?.LogFile, existingAppSettings?.Port ?? 4040);
            var settingsData = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
            System.IO.File.WriteAllText("settings.json", settingsData);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://twitchtokengenerator.com/");
        }

        private void btnGenerateToken_Click(object sender, EventArgs e)
        {
            var form = new TokenGeneratorForm(this);
            form.Show();
        }

        internal void OnAccessTokenReceived(TwitchOAuthResult e)
        {
            this.authResult = e;
            BeginInvoke(() =>
            {
                if (string.IsNullOrEmpty(inputChannel.Text))
                {
                    inputChannel.Text = e.User;
                }

                inputToken.Text = e.AccessToken;
            });
        }

        private void BeginInvoke(Action a)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new ActionInvoke(BeginInvoke), a);
            }
            else if (a != null)
            {
                a();
            }
        }

        public delegate void ActionInvoke(Action a);
    }
}
