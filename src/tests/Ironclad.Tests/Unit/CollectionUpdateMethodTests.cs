using System.Linq;
using Xunit;

namespace Ironclad.Tests.Unit
{
    using Console.Commands;

    public class CollectionUpdateMethodTests
    {
        [Fact]
        public void ApplyTo_Assign_ReturnsAssignedValues()
        {
            const int collectionLength = 3;

            var sourceCollection = Enumerable.Range(1, collectionLength).Select(x => x.ToString()).ToList();

            var changeCollection = Enumerable.Range(collectionLength + 1, collectionLength)
                .Select(x => x.ToString())
                .ToList();

            var updateMethod = CollectionUpdateMethod<string>.FromOptions(false, false, changeCollection);

            var resultCollection = updateMethod.ApplyTo(sourceCollection);

            Assert.True(changeCollection.SequenceEqual(resultCollection));
        }

        [Fact]
        public void ApplyTo_AddNotCrossedValues_ReturnsConcatenatedCollection()
        {
            const int collectionLength = 10;

            var sourceCollection = Enumerable.Range(1, collectionLength).Select(x => x.ToString()).ToList();

            var changeCollection = Enumerable.Range(collectionLength + 1, collectionLength)
                .Select(x => x.ToString())
                .ToList();

            var updateMethod = CollectionUpdateMethod<string>.FromOptions(true, false, changeCollection);

            var resultCollection = updateMethod.ApplyTo(sourceCollection);

            Assert.Equal(sourceCollection.Count + changeCollection.Count, resultCollection.Count);

            changeCollection.ForEach(x => Assert.True(resultCollection.Contains(x)));

            sourceCollection.ForEach(x => Assert.True(resultCollection.Contains(x)));
        }

        [Fact]
        public void ApplyTo_AddCrossedValues_ReturnsConcatenatedCollection()
        {
            // todo
        }

        [Fact]
        public void ApplyTo_RemoveCrossedValues_ReturnsOriginalCollectionButValuesRemoved()
        {
            // todo
        }

        [Fact]
        public void ApplyTo_RemoveNotCrossedValues_ReturnsOriginalCollectionButValuesRemoved()
        {
            // todo
        }

        [Fact]
        public void ApplyTo_InvalidOptions_ReturnsUnchangedCollection()
        {
            // todo
        }
    }
}