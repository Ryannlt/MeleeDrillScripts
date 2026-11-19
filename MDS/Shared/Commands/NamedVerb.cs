namespace MDS.Shared.Commands
{
    // rc <command> <name> <verb> <args>, the shape rc program and rc customdrill share.
    public sealed class NamedVerb
    {
        private const int VerbAt = 1;
        private const int ArgsAt = 2;

        public NamedVerb(string[] parameters)
        {
            Name = parameters.Length > 0 ? parameters[0] : string.Empty;
            Verb = parameters.Length > VerbAt ? parameters[VerbAt].ToLowerInvariant() : string.Empty;
            Args = parameters.Length > ArgsAt ? parameters[ArgsAt..] : new string[0];
        }

        public string Name { get; }

        // Lowercase, or empty when only a name was given.
        public string Verb { get; }

        public string[] Args { get; }
    }
}
