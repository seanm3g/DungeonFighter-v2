namespace RPGGame.Game.Testing.Commands
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for test menu commands.
    /// Implements the Command pattern to replace the large switch statement in TestingSystemHandler.
    /// </summary>
    public interface ITestCommand
    {
        /// <summary>
        /// Executes the test command.
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ExecuteAsync();
    }
}
