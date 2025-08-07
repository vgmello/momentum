// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Extensions;

namespace Momentum.Extensions.Tests.Extensions;

public class PluralizationServiceTests
{
    [Theory]
    // Regular words - add 's'
    [InlineData("cat", "cats")]
    [InlineData("dog", "dogs")]
    [InlineData("book", "books")]
    [InlineData("car", "cars")]
    [InlineData("house", "houses")]
    [InlineData("table", "tables")]
    // Words ending in sibilants - add 'es'
    [InlineData("box", "boxes")]
    [InlineData("bus", "buses")]
    [InlineData("glass", "glass")]
    [InlineData("class", "classes")]
    [InlineData("dish", "dishes")]
    [InlineData("brush", "brushes")]
    [InlineData("church", "churches")]
    [InlineData("watch", "watches")]
    [InlineData("fox", "foxes")]
    [InlineData("buzz", "buzzes")]
    [InlineData("data", "data")]
    // Words ending in consonant + y - change 'y' to 'ies'
    [InlineData("baby", "babies")]
    [InlineData("city", "cities")]
    [InlineData("story", "stories")]
    [InlineData("party", "parties")]
    [InlineData("lady", "ladies")]
    [InlineData("country", "countries")]
    [InlineData("cow", "cows")]
    [InlineData("family", "families")]
    [InlineData("company", "companies")]
    // Words ending in vowel + y - add 's'
    [InlineData("boy", "boys")]
    [InlineData("day", "days")]
    [InlineData("key", "keys")]
    [InlineData("toy", "toys")]
    [InlineData("play", "plays")]
    [InlineData("way", "ways")]
    [InlineData("monkey", "monkeys")]
    [InlineData("turkey", "turkeys")]
    // Words ending in f/fe - change to 'ves'
    [InlineData("leaf", "leaves")]
    [InlineData("knife", "knives")]
    [InlineData("life", "lives")]
    [InlineData("wife", "wives")]
    [InlineData("half", "halves")]
    [InlineData("shelf", "shelves")]
    [InlineData("wolf", "wolves")]
    [InlineData("calf", "calves")]
    [InlineData("self", "selves")]
    // Words ending in f/fe exceptions - add 's'
    [InlineData("roof", "roofs")]
    [InlineData("chief", "chiefs")]
    [InlineData("cliff", "cliffs")]
    [InlineData("staff", "staff")]
    [InlineData("safe", "safes")]
    [InlineData("cafe", "cafes")]
    // Words ending in consonant + o - add 'es'
    [InlineData("hero", "heroes")]
    [InlineData("potato", "potatoes")]
    [InlineData("tomato", "tomatoes")]
    [InlineData("echo", "echoes")]
    [InlineData("tornado", "tornadoes")]
    [InlineData("volcano", "volcanoes")]
    // Words ending in vowel + o - add 's'
    [InlineData("photo", "photos")]
    [InlineData("piano", "pianos")]
    [InlineData("radio", "radios")]
    [InlineData("video", "videos")]
    [InlineData("studio", "studios")]
    [InlineData("zoo", "zoos")]
    [InlineData("tattoo", "tattoos")]
    // Irregular words
    [InlineData("child", "children")]
    [InlineData("man", "men")]
    [InlineData("woman", "women")]
    [InlineData("foot", "feet")]
    [InlineData("tooth", "teeth")]
    [InlineData("mouse", "mice")]
    [InlineData("goose", "geese")]
    [InlineData("person", "people")]
    [InlineData("ox", "oxen")]
    // Unchanged words
    [InlineData("sheep", "sheep")]
    [InlineData("deer", "deer")]
    [InlineData("fish", "fish")]
    [InlineData("moose", "moose")]
    [InlineData("species", "species")]
    [InlineData("series", "series")]
    [InlineData("aircraft", "aircraft")]
    [InlineData("spacecraft", "spacecraft")]
    // Words ending in 'sis' - change to 'ses'
    [InlineData("analysis", "analyses")]
    [InlineData("basis", "bases")]
    [InlineData("crisis", "crises")]
    [InlineData("thesis", "theses")]
    [InlineData("diagnosis", "diagnoses")]
    [InlineData("oasis", "oases")]
    // Latin-derived words
    [InlineData("criterion", "criteria")]
    [InlineData("phenomenon", "phenomena")]
    [InlineData("datum", "data")]
    [InlineData("medium", "media")]
    [InlineData("curriculum", "curricula")]
    [InlineData("memorandum", "memoranda")]
    // Edge cases
    [InlineData("", "")]
    [InlineData(" ", " ")]
    // Case preservation
    [InlineData("CAT", "CATS")]
    [InlineData("Dog", "Dogs")]
    [InlineData("HOUSE", "HOUSES")]
    [InlineData("Ledger", "Ledgers")]
    [InlineData("Cashier", "Cashiers")]
    [InlineData("Invoice", "Invoices")]
    [InlineData("Accounting", "Accounting")]
    [InlineData("User", "Users")]
    [InlineData("Users", "Users")]
    [InlineData("UsersAccount", "UsersAccounts")]
    [InlineData("Permission", "Permissions")]
    [InlineData("Business", "Businesses")]
    [InlineData("Businesses", "Businesses")]
    [InlineData("Human", "Humans")]
    public void ToPlural_AllCases_ReturnsCorrectPlural(string value, string expectedPlural)
    {
        // Arrange & Act
        var result = value.Pluralize();

        // Assert
        result.ShouldBe(expectedPlural);
    }
}
