// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.LegacyDocs.Migration;

/// <summary>Handles mixed-type branch sequences: plain strings and alias dicts like <c>{alias: branch}</c>.</summary>
public class BranchRefListConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(List<BranchRef>);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var list = new List<BranchRef>();

		if (!parser.TryConsume<SequenceStart>(out _))
			return list;

		while (!parser.TryConsume<SequenceEnd>(out _))
		{
			if (parser.TryConsume<Scalar>(out var scalar))
			{
				list.Add(new BranchRef(scalar.Value));
			}
			else if (parser.TryConsume<MappingStart>(out _))
			{
				// {alias: branch} — first key-value pair wins
				var key = parser.Consume<Scalar>();
				var value = parser.Consume<Scalar>();
				list.Add(new BranchRef(Name: value.Value, Alias: key.Value));

				// Consume remaining pairs (shouldn't exist but be safe)
				while (!parser.TryConsume<MappingEnd>(out _))
				{
					_ = parser.Consume<Scalar>();
					_ = parser.Consume<Scalar>();
				}
			}
		}

		return list;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is not List<BranchRef> list)
			return;

		emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));

		foreach (var branch in list)
		{
			if (branch.Alias is not null)
			{
				emitter.Emit(new MappingStart(null, null, false, MappingStyle.Flow));
				emitter.Emit(new Scalar(branch.Alias));
				emitter.Emit(new Scalar(branch.Name));
				emitter.Emit(new MappingEnd());
			}
			else
			{
				emitter.Emit(new Scalar(branch.Name));
			}
		}

		emitter.Emit(new SequenceEnd());
	}
}

/// <summary>Handles a single <see cref="BranchRef"/> scalar or mapping node.</summary>
public class BranchRefConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(BranchRef);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.TryConsume<Scalar>(out var scalar))
			return new BranchRef(scalar.Value);

		if (parser.TryConsume<MappingStart>(out _))
		{
			var key = parser.Consume<Scalar>();
			var value = parser.Consume<Scalar>();
			_ = parser.Consume<MappingEnd>();
			return new BranchRef(Name: value.Value, Alias: key.Value);
		}

		throw new YamlException("Expected scalar or mapping for BranchRef");
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value is not BranchRef branch)
			return;

		if (branch.Alias is not null)
		{
			emitter.Emit(new MappingStart(null, null, false, MappingStyle.Flow));
			emitter.Emit(new Scalar(branch.Alias));
			emitter.Emit(new Scalar(branch.Name));
			emitter.Emit(new MappingEnd());
		}
		else
		{
			emitter.Emit(new Scalar(branch.Name));
		}
	}
}
