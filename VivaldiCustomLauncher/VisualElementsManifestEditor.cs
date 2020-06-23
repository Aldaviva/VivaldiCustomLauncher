using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace VivaldiCustomLauncher {

    public class VisualElementsManifestEditor {

        private readonly XmlSerializer xmlSerializer = new XmlSerializer(typeof(ApplicationManifest));

        public ApplicationManifest load(string filePath) {
            using Stream fileStream = new FileStream(filePath, FileMode.Open);
            return (ApplicationManifest) xmlSerializer.Deserialize(fileStream);
        }

        public void save(ApplicationManifest application, string filePath) {
            using var xmlWriter = XmlWriter.Create(filePath, new XmlWriterSettings {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = true,
                Encoding = new UTF8Encoding(false)
            });

            xmlSerializer.Serialize(xmlWriter, application, new XmlSerializerNamespaces(new[] {
                new XmlQualifiedName("xsi", XmlSchema.InstanceNamespace)
            }));
        }

        public void relativizeUris(ApplicationManifest application, string pathsRelativeTo) {
            string relativizeUri(string originalUri) => Path.Combine(pathsRelativeTo, originalUri);

            VisualElements visualElements = application.visualElements;
            visualElements.square150X150Logo = relativizeUri(visualElements.square150X150Logo);
            visualElements.square70X70Logo = relativizeUri(visualElements.square70X70Logo);
            visualElements.square44X44Logo = relativizeUri(visualElements.square44X44Logo);
        }

    }

    [XmlRoot(ElementName = "VisualElements")]
    public class VisualElements {

        [XmlAttribute(AttributeName = "ShowNameOnSquare150x150Logo")]
        public string showNameOnSquare150X150Logo { get; set; }

        [XmlAttribute(AttributeName = "Square150x150Logo")]
        public string square150X150Logo { get; set; }

        [XmlAttribute(AttributeName = "Square70x70Logo")]
        public string square70X70Logo { get; set; }

        [XmlAttribute(AttributeName = "Square44x44Logo")]
        public string square44X44Logo { get; set; }

        [XmlAttribute(AttributeName = "ForegroundText")]
        public string foregroundText { get; set; }

        [XmlAttribute(AttributeName = "BackgroundColor")]
        public string backgroundColor { get; set; }

        protected bool Equals(VisualElements other) {
            return showNameOnSquare150X150Logo == other.showNameOnSquare150X150Logo && square150X150Logo == other.square150X150Logo &&
                   square70X70Logo == other.square70X70Logo && square44X44Logo == other.square44X44Logo &&
                   foregroundText == other.foregroundText && backgroundColor == other.backgroundColor;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            return obj.GetType() == GetType() && Equals((VisualElements) obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (showNameOnSquare150X150Logo != null ? showNameOnSquare150X150Logo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (square150X150Logo != null ? square150X150Logo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (square70X70Logo != null ? square70X70Logo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (square44X44Logo != null ? square44X44Logo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (foregroundText != null ? foregroundText.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (backgroundColor != null ? backgroundColor.GetHashCode() : 0);
                return hashCode;
            }
        }

    }

    [XmlRoot(ElementName = "Application")]
    public class ApplicationManifest {

        [XmlElement(ElementName = "VisualElements")]
        public VisualElements visualElements { get; set; }

        protected bool Equals(ApplicationManifest other) {
            return Equals(visualElements, other.visualElements);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            return obj.GetType() == GetType() && Equals((ApplicationManifest) obj);
        }

        public override int GetHashCode() {
            return (visualElements != null ? visualElements.GetHashCode() : 0);
        }

    }

}