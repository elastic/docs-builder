using Elastic.LegacyDocs.Migration.Asciidoc;

var inputFile = args.Length > 0 ? args[0] : "test.adoc";
var opts = new AsciidocParserOptions
{
	FileReader = path => File.Exists(path) ? File.ReadAllText(path) : null,
	Attributes = new Dictionary<string, string>
	{
		["my-product"] = "Elasticsearch",
		["version"] = "8.16",
		["enterprise-only"] = ""
	}
};
var parser = new AsciidocParser(opts);
var doc = parser.Parse(inputFile);
var emitter = new MarkdownEmitter(new MarkdownEmitterOptions());
Console.Write(emitter.Emit(doc));
