﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using JSONAPI.Core;
using JSONAPI.Documents.Builders;
using JSONAPI.QueryableTransformers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JSONAPI.Tests.ActionFilters
{
    [TestClass]
    public class DefaultSortingTransformerTests : QueryableTransformerTestsBase
    {
        private class Dummy
        {
            // ReSharper disable UnusedAutoPropertyAccessor.Local
            public string Id { get; set; }
            // ReSharper restore UnusedAutoPropertyAccessor.Local

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public DateTime BirthDate { get; set; }
        }

        private IList<Dummy> _fixtures;
        private IQueryable<Dummy> _fixturesQuery;

        [TestInitialize]
        public void SetupFixtures()
        {
            _fixtures = new List<Dummy>
            {
                new Dummy {Id = "1", FirstName = "Thomas", LastName = "Paine", BirthDate = new DateTime(1737, 2, 9)},
                new Dummy {Id = "2", FirstName = "Samuel", LastName = "Adams", BirthDate = new DateTime(1722, 9, 27)},
                new Dummy {Id = "3", FirstName = "George", LastName = "Washington", BirthDate = new DateTime(1732, 2, 22)},
                new Dummy {Id = "4", FirstName = "Thomas", LastName = "Jefferson", BirthDate = new DateTime(1743, 4, 13)},
                new Dummy {Id = "5", FirstName = "Martha", LastName = "Washington", BirthDate = new DateTime(1731, 6, 13)},
                new Dummy {Id = "6", FirstName = "Abraham", LastName = "Lincoln", BirthDate = new DateTime(1809, 2, 12)},
                new Dummy {Id = "7", FirstName = "Andrew", LastName = "Jackson", BirthDate = new DateTime(1767, 3, 15)},
                new Dummy {Id = "8", FirstName = "Andrew", LastName = "Johnson", BirthDate = new DateTime(1808, 12, 29)},
                new Dummy {Id = "9", FirstName = "William", LastName = "Harrison", BirthDate = new DateTime(1773, 2, 9)}
            };
            _fixturesQuery = _fixtures.AsQueryable();
        }

        private DefaultSortingTransformer GetTransformer()
        {
            var pluralizationService = new PluralizationService(new Dictionary<string, string>
            {
                {"Dummy", "Dummies"}
            });
            var registrar = new ResourceTypeRegistrar(new DefaultNamingConventions(pluralizationService));
            var registration = registrar.BuildRegistration(typeof (Dummy));
            var registry = new ResourceTypeRegistry();
            registry.AddRegistration(registration);
            return new DefaultSortingTransformer(registry);
        }

        private Dummy[] GetArray(string uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            return GetTransformer().Sort(_fixturesQuery, request).ToArray();
        }

        private void RunTransformAndExpectFailure(string uri, string expectedMessage)
        {
            Action action = () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);

                // ReSharper disable once UnusedVariable
                var result = GetTransformer().Sort(_fixturesQuery, request).ToArray();
            };
            action.ShouldThrow<JsonApiException>().Which.Error.Detail.Should().Be(expectedMessage);
        }

        [TestMethod]
        public void Sorts_by_attribute_ascending()
        {
            var array = GetArray("http://api.example.com/dummies?sort=first-name");
            array.Should().BeInAscendingOrder(d => d.FirstName);
        }

        [TestMethod]
        public void Sorts_by_attribute_descending()
        {
            var array = GetArray("http://api.example.com/dummies?sort=-first-name");
            array.Should().BeInDescendingOrder(d => d.FirstName);
        }

        [TestMethod]
        public void Sorts_by_two_ascending_attributes()
        {
            var array = GetArray("http://api.example.com/dummies?sort=last-name,first-name");
            array.Should().ContainInOrder(_fixtures.OrderBy(d => d.LastName + d.FirstName));
        }

        [TestMethod]
        public void Sorts_by_two_descending_attributes()
        {
            var array = GetArray("http://api.example.com/dummies?sort=-last-name,-first-name");
            array.Should().ContainInOrder(_fixtures.OrderByDescending(d => d.LastName + d.FirstName));
        }

        [TestMethod]
        public void Returns_400_if_sort_argument_is_empty()
        {
            RunTransformAndExpectFailure("http://api.example.com/dummies?sort=", "One of the sort expressions is empty.");
        }

        [TestMethod]
        public void Returns_400_if_sort_argument_is_whitespace()
        {
            RunTransformAndExpectFailure("http://api.example.com/dummies?sort= ", "One of the sort expressions is empty.");
        }

        [TestMethod]
        public void Returns_400_if_sort_argument_is_empty_descending()
        {
            RunTransformAndExpectFailure("http://api.example.com/dummies?sort=-", "One of the sort expressions is empty.");
        }

        [TestMethod]
        public void Returns_400_if_sort_argument_is_whitespace_descending()
        {
            RunTransformAndExpectFailure("http://api.example.com/dummies?sort=- ", "One of the sort expressions is empty.");
        }

        [TestMethod]
        public void Returns_400_if_no_property_exists()
        {
            RunTransformAndExpectFailure("http://api.example.com/dummies?sort=foobar",
                "The attribute \"foobar\" does not exist on type \"dummies\".");
        }

        [TestMethod]
        public void Returns_400_if_the_same_property_is_specified_more_than_once()
        {
            RunTransformAndExpectFailure("http://api.example.com/dummies?sort=last-name,last-name",
                "The attribute \"last-name\" was specified more than once.");
        }

        [TestMethod]
        public void Can_sort_by_DateTimeOffset()
        {
            var array = GetArray("http://api.example.com/dummies?sort=birth-date");
            array.Should().BeInAscendingOrder(d => d.BirthDate);
        }
    }
}
