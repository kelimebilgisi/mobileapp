using System;
using FluentAssertions;
using Toggl.Ultrawave.Models;
using Xunit;

namespace Toggl.Ultrawave.Tests.Models
{
    public sealed class TagTests
    {
        private string validJson
            => "{\"id\":2024667,\"workspace_id\":424213,\"name\":\"mobile\",\"at\":\"2014-04-25T10:10:13+00:00\",\"deleted_at\":\"2014-04-25T10:10:10+00:00\"}";

        private string validJsonWithDefaultAtValue
            => "{\"id\":2024667,\"workspace_id\":424213,\"name\":\"mobile\",\"at\":\"1970-01-01T00:00:00+00:00\",\"deleted_at\":\"2014-04-25T10:10:10+00:00\"}";

        private Tag validTag => new Tag
        {
            Id = 2024667,
            WorkspaceId = 424213,
            Name = "mobile",
            At = new DateTimeOffset(2014, 04, 25, 10, 10, 13, TimeSpan.Zero),
            ServerDeletedAt = new DateTimeOffset(2014, 04, 25, 10, 10, 10, TimeSpan.Zero)
        };

        private Tag validTagWithDefaultAtValue => new Tag
        {
            Id = 2024667,
            WorkspaceId = 424213,
            Name = "mobile",
            At = new DateTimeOffset(1970, 01, 01, 00, 00, 00, TimeSpan.Zero),
            ServerDeletedAt = new DateTimeOffset(2014, 04, 25, 10, 10, 10, TimeSpan.Zero)
        };

        [Fact, LogIfTooSlow]
        public void HasConstructorWhichCopiesValuesFromInterfaceToTheNewInstance()
        {
            var clonedObject = new Tag(validTag);

            clonedObject.Should().NotBeSameAs(validTag);
            clonedObject.Should().BeEquivalentTo(validTag, options => options.IncludingProperties());
        }

        [Fact, LogIfTooSlow]
        public void CanBeDeserialized()
        {
            SerializationHelper.CanBeDeserialized(validJson, validTag);
        }

        [Fact, LogIfTooSlow]
        public void CanBeSerialized()
        {
            SerializationHelper.CanBeSerialized(validJson, validTag);
        }

        [Fact, LogIfTooSlow]
        public void CanBeDeserializedWithADefaultAtValue()
        {
            SerializationHelper.CanBeDeserialized(validJsonWithDefaultAtValue, validTagWithDefaultAtValue);
        }

        [Fact, LogIfTooSlow]
        public void CanBeSerializedWithADefaultAtValue()
        {
            SerializationHelper.CanBeSerialized(validJsonWithDefaultAtValue, validTagWithDefaultAtValue);
        }
    }
}
