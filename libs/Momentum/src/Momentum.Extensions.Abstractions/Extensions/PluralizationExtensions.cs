// Copyright (c) ABCDEG. All rights reserved.

namespace Momentum.Extensions.Abstractions.Extensions;

public static class PluralizationExtensions
{
    private static readonly Dictionary<string, string> IrregularNouns = new(StringComparer.OrdinalIgnoreCase)
    {
        // People and family
        { "man", "men" },
        { "woman", "women" },
        { "child", "children" },
        { "person", "people" },

        // Body parts
        { "tooth", "teeth" },
        { "foot", "feet" },
        { "goose", "geese" },

        // Animals
        { "mouse", "mice" },
        { "louse", "lice" },
        { "ox", "oxen" },

        // Other common irregular nouns
        { "die", "dice" },
        { "penny", "pence" },
        { "cactus", "cacti" },
        { "focus", "foci" },
        { "fungus", "fungi" },
        { "nucleus", "nuclei" },
        { "radius", "radii" },
        { "stimulus", "stimuli" },
        { "alumnus", "alumni" },
        { "bacillus", "bacilli" },
        { "curriculum", "curricula" },
        { "datum", "data" },
        { "stratum", "strata" },
        { "criterion", "criteria" },
        { "phenomenon", "phenomena" },
        { "index", "indices" },
        { "vertex", "vertices" },
        { "matrix", "matrices" },
        { "axis", "axes" },
        { "diagnosis", "diagnoses" },
        { "ellipsis", "ellipses" },
        { "hypothesis", "hypotheses" },
        { "oasis", "oases" },
        { "synopsis", "synopses" },
        { "thesis", "theses" },
        { "appendix", "appendices" },
        { "bureau", "bureaux" },
        { "tableau", "tableaux" },
        { "plateau", "plateaux" },
        { "syllabus", "syllabi" },
        { "octopus", "octopuses" },
        { "hippopotamus", "hippopotami" },
        { "platypus", "platypuses" },
        { "rhinoceros", "rhinoceroses" },
        { "millennium", "millennia" },
        { "memorandum", "memoranda" },
        { "bacterium", "bacteria" },
        { "medium", "media" },
        { "gymnasium", "gymnasia" },
        { "consortium", "consortia" },
        { "symposium", "symposia" },
        { "erratum", "errata" },
        { "addendum", "addenda" },
        { "alga", "algae" },
        { "vertebra", "vertebrae" },
        { "larva", "larvae" },
        { "antenna", "antennae" },
        { "formula", "formulae" },
        { "nebula", "nebulae" },
        { "vita", "vitae" },
        { "genus", "genera" },
        { "opus", "opera" },
        { "corpus", "corpora" },
        { "viscus", "viscera" }
    };

    private static readonly HashSet<string> IrregularNounsReverse =
        IrregularNouns.Select(kv => kv.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> UnchangingNouns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Animals
        "sheep", "fish", "deer", "moose", "salmon", "trout", "swine", "bison", "cod",
        "carp", "pike", "bass", "perch", "shrimp", "squid", "elk", "buffalo",

        // Nationalities
        "Chinese", "Japanese", "Vietnamese", "Portuguese", "Maltese", "Swiss",

        // Other invariant nouns
        "series", "species", "corps", "means", "crossroads", "headquarters",
        "gallows", "innings", "aircraft", "hovercraft", "spacecraft", "watercraft",
        "offspring", "grouse", "chassis", "precis", "apparatus", "staff"
    };

    private static readonly HashSet<string> UncountableNouns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Materials and substances
        "gold", "silver", "wood", "glass", "paper", "cotton", "wool", "silk",
        "plastic", "steel", "copper", "iron", "brass", "bronze", "aluminum",

        // Food and drink
        "water", "milk", "coffee", "tea", "sugar", "salt", "pepper", "flour",
        "rice", "pasta", "bread", "butter", "cheese", "meat", "beef", "pork",
        "bacon", "honey", "jam", "chocolate",

        // Abstract concepts
        "accounting", "information", "knowledge", "wisdom", "advice", "progress", "research",
        "evidence", "education", "intelligence", "patience", "pride", "courage",
        "honesty", "beauty", "love", "hate", "anger", "happiness", "sadness",
        "health", "wealth", "poverty", "music", "art", "poetry", "dancing",
        "singing", "homework", "housework", "work", "employment",

        // Others
        "money", "currency", "furniture", "equipment", "machinery", "luggage",
        "baggage", "garbage", "rubbish", "traffic", "transportation", "pollution",
        "electricity", "energy", "power", "time", "rain", "snow",
        "wind", "fog", "weather", "heat", "cold", "warmth", "humidity",
        "sunshine", "darkness", "lightning", "thunder", "gravity", "underwear",
        "software", "hardware", "leisure", "recreation", "fun", "laughter"
    };

    // Words where -f/-fe to -ves rule doesn't apply
    private static readonly HashSet<string> FExceptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "chief", "cliff", "proof", "reef", "roof", "belief", "chef",
        "safe", "giraffe", "cafe", "strife"
    };

    // Words ending in -o that take -es
    private static readonly HashSet<string> OesToEs = new(StringComparer.OrdinalIgnoreCase)
    {
        "hero", "potato", "tomato", "echo", "torpedo", "veto", "embargo",
        "volcano", "tornado", "mosquito", "domino", "mango"
    };

    private static readonly HashSet<char> Vowels = ['a', 'e', 'i', 'o', 'u'];

    private static readonly HashSet<string> PluralIndicatingSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "er", "or", "ar", // users, computers, actors, doctors, cars, bars
        "le", "ge", "te", "se", "ce", "de", "ne", "me", "pe", "re", "ve", "we", "ze", // tables, pages, dates, houses, etc.
        "al", "el", "il", "ol", "ul", // animals, models, emails, tools, urls
        "an", "en", "in", "on", "un", // plans, tokens, admins, actions, nouns
        "at", "et", "it", "ot", "ut" // formats, assets, units, robots, inputs
    };

    public static string Pluralize(this string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
            return word;

        word = word.Trim();

        if (UncountableNouns.Contains(word) || UnchangingNouns.Contains(word) || !ShouldPluralize(word))
            return word;

        // Preserve original casing pattern
        var isUpperCase = char.IsUpper(word[0]);
        var isAllCaps = word.All(c => !char.IsLetter(c) || char.IsUpper(c));

        var result = IrregularNouns.TryGetValue(word, out var irregular) ? irregular : ApplyRegularPluralizationRules(word);

        if (isAllCaps)
            return result.ToUpperInvariant();

        if (isUpperCase && result.Length > 0)
        {
            return char.ToUpper(result[0]) + result[1..];
        }

        return result;
    }

    private static string ApplyRegularPluralizationRules(string word)
    {
        var length = word.Length;

        // Rule: Words ending in -is (analysis -> analyses)
        if (length > 2 && word.EndsWith("is", StringComparison.OrdinalIgnoreCase))
            return word[..(length - 2)] + "es";

        // Rule: Words ending in -us (handle specific cases)
        if (length > 2 && word.EndsWith("us", StringComparison.OrdinalIgnoreCase) &&
            !word.EndsWith("ous", StringComparison.OrdinalIgnoreCase))
        {
            // Most common -us words just add -es (bonus -> bonuses)
            return word + "es";
        }

        // Rule: Words ending in consonant + -y
        if (word[length - 1] == 'y')
        {
            var beforeY = word[length - 2];

            if (!Vowels.Contains(beforeY))
                return word[..(length - 1)] + "ies";

            return word + "s";
        }

        // Rule: Words ending in -o
        if (word[length - 1] == 'o')
        {
            // Check if it's a word that needs -es
            if (OesToEs.Contains(word))
                return word + "es";

            // Default for -o endings
            return word + "s";
        }

        // Rule: Words ending in -s, -x, -z, -ch, -sh
        if (HasSibilantSuffix(word))
            return word + "es";

        // Rule: Words ending in -f or -fe
        if (word.EndsWith("f", StringComparison.OrdinalIgnoreCase) || word.EndsWith("fe", StringComparison.OrdinalIgnoreCase))
        {
            if (!FExceptions.Contains(word))
            {
                if (word.EndsWith("f", StringComparison.OrdinalIgnoreCase))
                {
                    return word[..(length - 1)] + "ves";
                }

                // ends with "fe"
                return word[..(length - 2)] + "ves";
            }

            return word + "s"; // For exceptions like "roof" -> "roofs"
        }

        // Default: Add -s
        return word + "s";
    }

    private static bool ShouldPluralize(string word)
    {
        // Check if it's an irregular plural
        if (IrregularNounsReverse.Contains(word))
            return false;

        // Check if it's an unchanging noun (could be singular or plural)
        if (UnchangingNouns.Contains(word))
            return false;

        // Check if it's uncountable
        if (UncountableNouns.Contains(word))
            return false;

        return !IsPluralByRules(word);
    }

    private static bool IsPluralByRules(string word)
    {
        var length = word.Length;

        // Check for -ies ending (countries, flies, etc.)
        if (length > 3 && word.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for -ves ending (lives, knives, etc.)
        if (length > 3 && word.EndsWith("ves", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for -ses ending (analyses, bases, etc.)
        if (length > 3 && word.EndsWith("ses", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for -xes, -zes, -ches, -shes endings
        if (HasSibilantPluralSuffix(word))
            return true;

        // Check for -oes ending (heroes, potatoes, etc.)
        if (length > 3 && word.EndsWith("oes", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for words ending in -s (but not -ss)
        if (word.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
        {
            // Additional checks to avoid false positives
            var singularCandidate = word[..^1]; // Remove the 's'

            if (singularCandidate.Length < 2)
                return false;

            var lastTwoChars = singularCandidate[^2..];

            return PluralIndicatingSuffixes.Contains(lastTwoChars);
        }

        return false;
    }

    /// <summary>
    ///     Checks if a word ends with a sibilant sound (s, x, z, ch, sh).
    /// </summary>
    private static bool HasSibilantSuffix(string word) =>
        word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
        word.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
        word.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
        word.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
        word.EndsWith("sh", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Checks if a word is the plural form of a word ending with a sibilant sound.
    /// </summary>
    private static bool HasSibilantPluralSuffix(string word) =>
        word.EndsWith("ses", StringComparison.OrdinalIgnoreCase) ||
        word.EndsWith("xes", StringComparison.OrdinalIgnoreCase) ||
        word.EndsWith("zes", StringComparison.OrdinalIgnoreCase) ||
        word.EndsWith("ches", StringComparison.OrdinalIgnoreCase) ||
        word.EndsWith("shes", StringComparison.OrdinalIgnoreCase);
}
