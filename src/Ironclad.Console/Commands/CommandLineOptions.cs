// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Ironclad.Console.Commands
{
    using McMaster.Extensions.CommandLineUtils;

    public class CommandLineOptions
    {
        public CommandOption Help { get; private set; }

        public CommandOption Verbose { get; private set; }

        public ICommand Command { get; set; }

        public static CommandLineOptions Parse(string[] args, IConsole console)
        {
            // NOTE (Cameron): We need to pass the options through each of the commands before we can evaluate the result of the parsing.
            var options = new CommandLineOptions();

            var app = new CommandLineApplication(console);

            options.Verbose = app.VerboseOption();
            options.Help = app.HelpOption();

            // commands
            app.Command("login", command => LoginCommand.Configure(command, options, console));
            app.Command("clients", command => ClientsOptions.Configure(command, options, console));
            app.Command("apis", command => ApisOptions.Configure(command, options, console));
            app.Command("users", command => UsersOptions.Configure(command, options, console));
            app.Command("roles", command => RolesOptions.Configure(command, options));
            app.Command("scopes", command => ScopesOptions.Configure(command, options, console));
            app.Command("providers", command => IdentityProvidersOptions.Configure(command, options, console));

            // action (for this command)
            app.OnExecute(() => app.ShowVersionAndHelp());

            // NOTE (Cameron): The result of a successful execute will assign a command to the options which gets invoked elsewhere.
            if (app.Execute(args) != 0)
            {
                // when command line parsing error in subcommand
                return null;
            }

            return options;
        }
    }
}
