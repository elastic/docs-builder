// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.FrontMatter;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives;

public class AppliesBlock(DirectiveBlockParser parser, Dictionary<string, string> properties)
	: DirectiveBlock(parser, properties)
{
	public override string Directive => "mermaid";

	public Deployment? Deployment { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (TryGetAvailability("stack", out var version))
		{
			Deployment ??= new Deployment();
			Deployment.SelfManaged ??= new SelfManagedDeployment();
			Deployment.SelfManaged.Stack = version;
		}
		if (TryGetAvailability("ece", out version))
		{
			Deployment ??= new Deployment();
			Deployment.SelfManaged ??= new SelfManagedDeployment();
			Deployment.SelfManaged.Ece = version;
		}
		if (TryGetAvailability("eck", out version))
		{
			Deployment ??= new Deployment();
			Deployment.SelfManaged ??= new SelfManagedDeployment();
			Deployment.SelfManaged.Eck = version;
		}
		if (TryGetAvailability("hosted", out version))
		{
			Deployment ??= new Deployment();
			Deployment.Cloud ??= new CloudManagedDeployment();
			Deployment.Cloud.Hosted = version;
		}
		if (TryGetAvailability("serverless", out version))
		{
			Deployment ??= new Deployment();
			Deployment.Cloud ??= new CloudManagedDeployment();
			Deployment.Cloud.Serverless = version;
		}

		if (Deployment is null)
			EmitError(context, "{applies} block with no product availability specified");

		var index = Parent?.IndexOf(this);
		if (Parent is not null && index > 0)
		{
			var i = index - 1 ?? 0;
			var prevSib = Parent[i];
			if (prevSib is not HeadingBlock)
				EmitError(context, "{applies} should follow a heading");
		}

		bool TryGetAvailability(string key, out ProductAvailability? semVersion)
		{
			semVersion = null;
			return Prop(key) is {} v && ProductAvailability.TryParse(v, out semVersion);
		}
	}
}
