using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UpSkope
{
    public abstract class Command : IEquatable<Command>
    {
        [JsonIgnore]
        public virtual string CommandId { get; set; }

        [JsonIgnore]
        public bool Encrypt { get; set; }

        public class EncryptedCommandData
        {
            [JsonPropertyName("encryptData")]
            public string Data { get; set; } = "";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Command);
        }

        public bool Equals(Command other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetType() != other.GetType()) return false;

            // Compare all non-ignored properties
            var properties = GetType().GetProperties()
                .Where(p => p.Name != "CommandId" && p.Name != "Encrypt" && !p.GetCustomAttributes<JsonIgnoreAttribute>().Any());

            foreach (var prop in properties)
            {
                var thisValue = prop.GetValue(this);
                var otherValue = prop.GetValue(other);

                if (thisValue == null && otherValue == null) continue;
                if (thisValue == null || otherValue == null) return false;
                if (!thisValue.Equals(otherValue)) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var properties = GetType().GetProperties()
                .Where(p => p.Name != "CommandId" && p.Name != "Encrypt" && !p.GetCustomAttributes<JsonIgnoreAttribute>().Any());

            unchecked
            {
                int hash = 17;
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(this);
                    hash = hash * 23 + (value?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        public static bool operator ==(Command left, Command right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(Command left, Command right)
        {
            return !(left == right);
        }

        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            if (Encrypt)
            {
                var crypto = new Crypto();

                // Create a dictionary with just the command data
                var commandDict = new Dictionary<string, object>();
                foreach (var prop in GetType().GetProperties())
                {
                    if (prop.Name != "CommandId" && prop.Name != "Encrypt" && !prop.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                    {
                        commandDict[prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name] = prop.GetValue(this);
                    }
                }
                string json = JsonSerializer.Serialize(commandDict, options);
                string encrypted = crypto.EncryptString(json);
                Console.WriteLine("Before encryption: " + json);
                Console.WriteLine("After encryption: " + encrypted);
                var encryptedCommand = new EncryptedCommand { CommandId = CommandId, Data = encrypted };
                var dict = new Dictionary<string, object> { [CommandId] = encryptedCommand };
                return JsonSerializer.Serialize(dict, options);
            }
            else
            {
                // Create a dictionary with just the command data (excluding CommandId)
                var commandDict = new Dictionary<string, object>();
                foreach (var prop in GetType().GetProperties())
                {
                    if (prop.Name != "CommandId" && prop.Name != "Encrypt" && !prop.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                    {
                        commandDict[prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name] = prop.GetValue(this);
                    }
                }
                var dict = new Dictionary<string, object> { [CommandId] = commandDict };
                return JsonSerializer.Serialize(dict, options);
            }
        }

        public static Command FromJson(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Parse the JSON to get the command ID
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var commandId = root.EnumerateObject().First().Name;
            var value = root.EnumerateObject().First().Value;

            // Check if it's an encrypted command
            if (value.TryGetProperty("encryptData", out var encryptedData))
            {
                var crypto = new Crypto();
                string decrypted = crypto.DecryptString(encryptedData.GetString() ?? "");

                // Find the command type based on the command ID
                var commandType = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract)
                    .FirstOrDefault(t => {
                        var instance = Activator.CreateInstance(t) as Command;
                        return instance?.CommandId == commandId;
                    })
                    ?? throw new JsonException($"Unknown command type: {commandId}");

                var command = (Command)Activator.CreateInstance(commandType);
                var commandData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(decrypted);
                foreach (var prop in commandType.GetProperties())
                {
                    if (prop.Name != "CommandId" && prop.Name != "Encrypt" && !prop.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                    {
                        var propName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
                        if (commandData.TryGetValue(propName, out var element))
                        {
                            prop.SetValue(command, JsonSerializer.Deserialize(element.GetRawText(), prop.PropertyType));
                        }
                    }
                }
                return command;
            }

            // Find the command type based on the command ID
            var type = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract)
                .FirstOrDefault(t => {
                    var instance = Activator.CreateInstance(t) as Command;
                    return instance?.CommandId == commandId;
                })
                ?? throw new JsonException($"Unknown command type: {commandId}");

            var command2 = (Command)Activator.CreateInstance(type);
            var commandData2 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText());
            foreach (var prop in type.GetProperties())
            {
                if (prop.Name != "CommandId" && prop.Name != "Encrypt" && !prop.GetCustomAttributes<JsonIgnoreAttribute>().Any())
                {
                    var propName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
                    if (commandData2.TryGetValue(propName, out var element))
                    {
                        prop.SetValue(command2, JsonSerializer.Deserialize(element.GetRawText(), prop.PropertyType));
                    }
                }
            }
            return command2;
        }

        public static string EncryptRawJson(string commandId, string rawJson)
        {
            var crypto = new Crypto();
            Console.WriteLine("Encrypting JSON: " + rawJson);
            string encrypted = crypto.EncryptString(rawJson);

            // Build the encrypted command structure
            var encryptedCommand = new Dictionary<string, object>
            {
                [commandId] = new Dictionary<string, object>
                {
                    ["encryptData"] = encrypted
                }
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(encryptedCommand, options);
        }
    }

    public class IdpTokenCommand : Command
    {
        public override string CommandId { get; set; } = "148";

        [JsonPropertyName("idpTokenValue")]
        public string TokenValue { get; set; } = "";

        [JsonPropertyName("tenantName")]
        public string TenantName { get; set; } = "";

        [JsonPropertyName("clientName")]
        public string ClientName { get; set; } = "nsClientUI_s2";

        [JsonPropertyName("sessionID")]
        public string SessionID { get; set; } = "2";
    }

    public class CaptureCommand : Command
    {
        public override string CommandId { get; set; } = "120";

        [JsonPropertyName("Capture")]
        public int Value { get; set; }
    }

    public class LogSizeCommand : Command
    {
        public override string CommandId { get; set; } = "157";

        [JsonPropertyName("logsize")]
        public int Value { get; set; }
    }

    public class EncryptedCommand : Command
    {
        [JsonPropertyName("encryptData")]
        public string Data { get; set; } = "";
    }
}