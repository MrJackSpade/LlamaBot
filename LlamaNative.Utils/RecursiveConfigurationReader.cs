using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Loxifi;
using LlamaNative.Utils.Extensions;

namespace LlamaNative.Utils
{
    public class RecursiveConfigurationReader<TConfiguration>
    {
        private string _root;

        public RecursiveConfigurationReader(string root)
        {
            _root = root;
        }

        public IEnumerable<string> Configurations
        {
            get
            {
                foreach (string directory in Directory.GetDirectories(_root, "*", SearchOption.AllDirectories))
                {
                    int subdirs = Directory.GetDirectories(directory).Length;

                    if (subdirs == 0)
                    {
                        yield return new DirectoryInfo(directory).Name;
                    }
                }
            }
        }

        private JsonSerializerOptions Options
        {
            get
            {
                JsonSerializerOptions options = new();
                options.Converters.Add(new JsonStringEnumConverter());
                return options;
            }
        }

        public RecursiveConfiguration<TConfiguration> Read(string characterName)
        {
            string characterDirectory = this.Find(characterName);

            TConfiguration typedConfiguration = this.BuildJson(characterDirectory);

            Stack<string> directories = new();

            DirectoryInfo characterDirectoryInfo = new(characterDirectory);

            do
            {
                directories.Push(characterDirectoryInfo.FullName);
                characterDirectoryInfo = characterDirectoryInfo.Parent;
            } while (characterDirectoryInfo.Parent.FullName != new DirectoryInfo(_root).FullName);

            Dictionary<string, string> resources = [];

            while (directories.Count > 0)
            {
                string currentDirectory = directories.Pop();

                foreach (string file in Directory.EnumerateFiles(currentDirectory))
                {
                    resources.TryAdd(Path.GetFileName(file), File.ReadAllText(file));
                }
            }

            return new RecursiveConfiguration<TConfiguration>()
            {
                Configuration = typedConfiguration,
                Resources = resources
            };
        }

        public TConfiguration BuildJson(string path)
        {
            Stack<string> configPaths = new();

            JsonObject jObject = new();

            foreach (string config in this.FindFiles(path, "Configuration.json"))
            {
                configPaths.Push(config);
            }

            while (configPaths.Any())
            {
                string thisConfigPath = configPaths.Pop();

                StringBuilder uncommented = new();

                foreach (string line in File.ReadAllLines(thisConfigPath))
                {
                    string pline = line;

                    if (!line.Trim().StartsWith("//"))
                    {
                        if (line.Contains("//"))
                        {
                            pline = pline.To("//")!;
                        }

                        uncommented.AppendLine(pline);
                    }
                }

                string configContent = uncommented.ToString();

                JsonObject cObject = (JsonObject)JsonNode.Parse(configContent);

                this.CopyOver(cObject, jObject);
            }

            string combinedString = jObject.ToString();

            return JsonSerializer.Deserialize<TConfiguration>(combinedString, this.Options);
        }

        public IEnumerable<string> FindFiles(string path, string fileName)
        {
            DirectoryInfo directory = new(path);

            bool found = false;
            do
            {
                string toCheck = Path.Combine(directory.FullName, fileName);

                if (File.Exists(toCheck))
                {
                    found = true;
                    yield return toCheck;
                }

                directory = directory.Parent;
            } while (directory != null);

            if (!found)
            {
                throw new FileNotFoundException($"File {fileName} not found in {path} or any parent directory");
            }
        }

        public string Find(string configurationName)
        {
            foreach (string directory in Directory.EnumerateDirectories(Path.Combine(AppContext.BaseDirectory, _root), "*", SearchOption.AllDirectories))
            {
                if (new DirectoryInfo(directory).Name == configurationName)
                {
                    return directory;
                }
            }

            throw new DirectoryNotFoundException();
        }

        private void CopyOver(JsonObject source, JsonObject destination)
        {
            foreach (KeyValuePair<string, JsonNode?> property in source)
            {
                if (property.Value is JsonObject cSource && destination[property.Key] is JsonObject cDest)
                {
                    this.CopyOver(cSource, cDest);
                }
                else
                {
                    destination[property.Key] = property.Value.CopyNode();
                }
            }
        }
    }
}

