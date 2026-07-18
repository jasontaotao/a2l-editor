using A2lEditor.Core.Model;

namespace A2lEditor.Core.Serialization;

/// <summary>
/// Serializes <see cref="A2lDocument"/> to/from JSON and XML.
/// </summary>
public interface IA2lDocumentSerializer
{
    /// <summary>Serialize document to indented JSON.</summary>
    string SerializeToJson(A2lDocument doc);

    /// <summary>Deserialize document from JSON.</summary>
    A2lDocument DeserializeFromJson(string json);

    /// <summary>Serialize document to indented XML.</summary>
    string SerializeToXml(A2lDocument doc);

    /// <summary>Deserialize document from XML.</summary>
    A2lDocument DeserializeFromXml(string xml);
}
