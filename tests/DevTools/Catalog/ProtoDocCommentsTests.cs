using System.IO;
using DevTools.Catalog;
using Xunit;

namespace DevTools.Catalog.Tests;

/// <summary>
/// Tests for ProtoDocComments parser.
/// </summary>
public class ProtoDocCommentsTests
{
    [Fact]
    public void Parse_IncludesMapFieldDescription()
    {
        // Arrange: proto file with a documented map field
        var protoContent = @"syntax = ""proto3"";

package test.v1;

// This is a test service.
service TestService {
  // Gets user by ID.
  rpc GetUser(GetUserRequest) returns (GetUserResponse);
}

// Request message for GetUser.
message GetUserRequest {
  // The user ID to look up.
  string user_id = 1;
  
  // Map field with documentation.
  // This map stores additional metadata.
  map<string, string> metadata = 2;
}

message GetUserResponse {
  string name = 1;
  string email = 2;
}
";
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, protoContent);

        try
        {
            // Act
            var comments = ProtoDocComments.Parse(tempPath);

            // Assert - parser uses format: service, service.method, message, message.field
            Assert.Contains("TestService", comments);
            Assert.Equal("This is a test service.", comments["TestService"]);

            // RPC methods use service.method format
            Assert.Contains("TestService.GetUser", comments);
            Assert.Equal("Gets user by ID.", comments["TestService.GetUser"]);

            Assert.Contains("GetUserRequest", comments);
            Assert.Equal("Request message for GetUser.", comments["GetUserRequest"]);

            // Regular field - key is messageName.fieldName
            var userIdEntry = comments.FirstOrDefault(kvp => kvp.Key.Contains("user_id"));
            Assert.NotEqual(default, userIdEntry);
            Assert.Equal("The user ID to look up.", userIdEntry.Value);

            // Critical assertion: map field description should be included
            var metadataEntry = comments.FirstOrDefault(kvp => kvp.Key.EndsWith(".metadata"));
            Assert.NotEqual(default, metadataEntry);
            Assert.Equal("Map field with documentation. This map stores additional metadata.", metadataEntry.Value);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Parse_ExcludesOptionDeclarations()
    {
        // Arrange: proto file with option declarations
        var protoContent = @"syntax = ""proto3"";

package test.v1;

// Option with documentation should not be treated as a field.
option go_package = ""test.v1"";
option deprecated = true;

message TestMessage {
  // A regular field.
  string name = 1;
}
";
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, protoContent);

        try
        {
            // Act
            var comments = ProtoDocComments.Parse(tempPath);

            // Assert: option declarations should not appear as keys
            Assert.DoesNotContain("option", comments.Keys);
            Assert.DoesNotContain("go_package", comments.Keys);
            Assert.DoesNotContain("deprecated", comments.Keys);
            
            // Regular field should still be included
            var nameEntry = comments.FirstOrDefault(kvp => kvp.Key.Contains("name"));
            Assert.NotEqual(default, nameEntry);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Parse_ExcludesReservedDeclarations()
    {
        // Arrange: proto file with reserved declarations
        var protoContent = @"syntax = ""proto3"";

package test.v1;

message TestMessage {
  reserved 1, 2, 3;
  reserved ""foo"", ""bar"";
  
  // A regular field.
  string name = 4;
}
";
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, protoContent);

        try
        {
            // Act
            var comments = ProtoDocComments.Parse(tempPath);

            // Assert: reserved declarations should not appear as keys
            Assert.DoesNotContain("reserved", comments.Keys);
            
            // Regular field should be included
            var nameEntry = comments.FirstOrDefault(kvp => kvp.Key.Contains("name"));
            Assert.NotEqual(default, nameEntry);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Parse_HandlesComplexMapField()
    {
        // Arrange: proto file with typed map fields
        var protoContent = @"syntax = ""proto3"";

package complex.v1;

message ComplexMessage {
  // Simple string map.
  map<string, string> simple_map = 1;
  
  // Nested message map.
  map<string, InnerMessage> nested_map = 2;
  
  // Repeated field for comparison.
  repeated string items = 3;
}

message InnerMessage {
  string value = 1;
}
";
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, protoContent);

        try
        {
            // Act
            var comments = ProtoDocComments.Parse(tempPath);

            // Assert: all fields should be included with their comments
            var simpleMapEntry = comments.FirstOrDefault(kvp => kvp.Key.Contains("simple_map"));
            Assert.NotEqual(default, simpleMapEntry);
            Assert.Equal("Simple string map.", simpleMapEntry.Value);

            var nestedMapEntry = comments.FirstOrDefault(kvp => kvp.Key.Contains("nested_map"));
            Assert.NotEqual(default, nestedMapEntry);
            Assert.Equal("Nested message map.", nestedMapEntry.Value);

            var itemsEntry = comments.FirstOrDefault(kvp => kvp.Key.Contains("items"));
            Assert.NotEqual(default, itemsEntry);
            Assert.Equal("Repeated field for comparison.", itemsEntry.Value);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}