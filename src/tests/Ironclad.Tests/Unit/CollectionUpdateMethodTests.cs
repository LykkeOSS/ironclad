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

        [Theory]
        [InlineData(10, 1)]
        [InlineData(10, 0)]
        [InlineData(10, 10)]
        public void ApplyTo_AddValues_ReturnsConcatenatedCollection(
            int collectionLength, 
            int crossValuesCount)
        {
            var sourceCollection = Enumerable.Range(1, collectionLength).Select(x => x.ToString()).ToList();

            var changeCollection = Enumerable.Range(collectionLength + 1 - crossValuesCount, collectionLength)
                .Select(x => x.ToString())
                .ToList();

            var updateMethod = CollectionUpdateMethod<string>.FromOptions(true, false, changeCollection);

            var resultCollection = updateMethod.ApplyTo(sourceCollection);

            Assert.Equal(2 * collectionLength - crossValuesCount, resultCollection.Count);

            changeCollection.ForEach(x => Assert.True(resultCollection.Contains(x)));

            sourceCollection.ForEach(x => Assert.True(resultCollection.Contains(x)));
        }

        [Theory]
        [InlineData(10, 1)]
        [InlineData(10, 0)]
        [InlineData(10, 10)]
        public void ApplyTo_RemoveValues_ReturnsOriginalCollectionButValuesRemoved(
            int collectionLength,
            int crossValuesCount)
        {
            var sourceCollection = Enumerable.Range(1, collectionLength).Select(x => x.ToString()).ToList();

            var changeCollection = Enumerable.Range(collectionLength + 1 - crossValuesCount, collectionLength)
                .Select(x => x.ToString())
                .ToList();

            var updateMethod = CollectionUpdateMethod<string>.FromOptions(false, true, changeCollection);

            var resultCollection = updateMethod.ApplyTo(sourceCollection);

            Assert.Equal(collectionLength - crossValuesCount, resultCollection.Count);

            changeCollection.ForEach(x => Assert.False(resultCollection.Contains(x)));
        }

        [Fact]
        public void ApplyTo_InvalidOptions_ReturnsUnchangedCollection()
        {
            const int collectionLength = 10;

            var sourceCollection = Enumerable.Range(1, collectionLength).Select(x => x.ToString()).ToList();

            var changeCollection = Enumerable.Range(collectionLength + 1, collectionLength)
                .Select(x => x.ToString())
                .ToList();

            var updateMethod = CollectionUpdateMethod<string>.FromOptions(true, true, changeCollection);

            var resultCollection = updateMethod.ApplyTo(sourceCollection);

            Assert.True(sourceCollection.SequenceEqual(resultCollection));
        }
    }
}