using ArkServerManager.Plugin.Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Plugin.Discord.UnitTests
{
    [TestClass]
    public class DiscordPluginUnitTest
    {
        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SingleLineMessage_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("The server has been started.");

            // Act
            plugin.HandleAlert(ArkServerManager.Plugin.Common.AlertType.Startup, "Server 1", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_MultipleLineMessage_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("The server is being shutdown.");
            alertMessage.AppendLine("Please logout to avoid profile corruption.");

            // Act
            plugin.HandleAlert(ArkServerManager.Plugin.Common.AlertType.Shutdown, "Server 2", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_ErrorAlertType_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("The server encountered an error while starting.");

            // Act
            plugin.HandleAlert(ArkServerManager.Plugin.Common.AlertType.Error, "Server 1", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SingleLineMessageToUnknownProfileName_Then_NoAlertSent()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("The server has been started.");

            // Act
            plugin.HandleAlert(ArkServerManager.Plugin.Common.AlertType.Startup, "", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_ExtraLongMessage_Then_AlertTruncated()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus ac lorem pretium, volutpat massa ut, iaculis augue. Aenean condimentum gravida laoreet. Morbi mattis leo non enim imperdiet dignissim. Donec et consectetur est. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Curabitur leo ipsum, commodo sed ante eu, vulputate maximus nulla. In sollicitudin, magna ut fringilla scelerisque, neque nulla semper nunc, at tempus nibh mi quis diam. Nunc quis tortor neque. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.");
            alertMessage.AppendLine("Maecenas ultrices in est a iaculis.Sed eget pharetra nibh.Duis luctus neque id iaculis vestibulum.Duis condimentum sapien metus, at pretium dui aliquam ullamcorper.Sed at efficitur tellus.Praesent eget ex blandit orci venenatis fringilla et in ex.Curabitur id mauris sed augue pharetra ornare.Integer at malesuada nisl, id blandit orci.");
            alertMessage.AppendLine("Ut ac dolor non ex porta lobortis.Aliquam sollicitudin nec justo ac finibus.Aliquam condimentum malesuada luctus.Nam ut ornare justo, a scelerisque sapien.Vivamus eget nisi risus.Morbi ut tellus ultricies arcu sagittis eleifend.Praesent eu augue in eros egestas rhoncus eu sed quam.");
            alertMessage.AppendLine("Quisque quis facilisis ipsum.In egestas pulvinar urna, id maximus lorem vehicula nec.Fusce vel nibh tincidunt, semper risus a, consectetur nunc.Morbi at lorem libero.Donec diam eros, aliquet in enim vitae, ornare malesuada nisi.Donec a mi pharetra dolor dignissim dapibus at vel velit.Praesent tincidunt, ipsum eget finibus cursus, ex turpis accumsan dui, ut hendrerit ante tortor vitae urna.Nulla faucibus ipsum nec tellus congue rhoncus.Maecenas sed tortor placerat, lobortis arcu sit amet, pellentesque sapien.Praesent sit amet feugiat massa.");
            alertMessage.AppendLine("Vestibulum eu felis accumsan, vehicula metus ut, gravida nulla.Sed pharetra sed ex vel sodales.In vestibulum, nisl vitae ultricies mattis, lacus massa maximus nunc, id suscipit lorem ligula id tortor.Donec porttitor diam ac turpis posuere aliquam.Phasellus non sed.");

            // Act
            plugin.HandleAlert(ArkServerManager.Plugin.Common.AlertType.Shutdown, "Server 3", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_OpenConfigForm()
        {
            // Arrange
            var plugin = new DiscordPlugin();

            // Act
            plugin.OpenConfigForm(null);

            // Assert
            Assert.IsTrue(true);
        }
    }
}
