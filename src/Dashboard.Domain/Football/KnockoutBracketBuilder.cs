namespace Dashboard.Domain.Football;

/// <summary>
/// Baut aus einer flachen Fixture-Menge den K.o.-Baum (rein &amp; testbar). Berücksichtigt nur Runden mit
/// zwei feststehenden Teams (TBD-Partien noch nicht ausgespielter Runden werden übersprungen). Innerhalb
/// einer Stage werden Spiele nach ungeordnetem Team-Paar gruppiert: ein Spiel = K.o.-Einzelspiel
/// (WM/EM), zwei Spiele = Hin-/Rückspiel (CL). Der Sieger ergibt sich aus dem Tor-Aggregat; bei
/// Gleichstand entscheidet das Elfmeterschießen des betreffenden Spiels.
/// </summary>
public static class KnockoutBracketBuilder
{
    // Reihenfolge über CL- und Turnier-Stages hinweg (frühe Runde zuerst).
    private static readonly string[] StageOrder =
        ["PLAYOFFS", "LAST_32", "LAST_16", "QUARTER_FINALS", "SEMI_FINALS", "THIRD_PLACE", "FINAL"];

    public static KnockoutBracket Build(IEnumerable<Fixture> fixtures)
    {
        var knockout = fixtures
            .Where(f => StageOrder.Contains(f.Stage) && f.TeamsKnown)
            .ToList();

        var rounds = new List<KnockoutRound>();
        IReadOnlyList<KnockoutTie>? previousMainRound = null;
        foreach (var stage in StageOrder)
        {
            var legs = knockout.Where(f => f.Stage == stage).ToList();
            if (legs.Count == 0)
            {
                continue;
            }

            var ties = legs
                .GroupBy(f => UnorderedPair(f.Home.Id, f.Away.Id))
                .Select(group => BuildTie(stage, group.OrderBy(f => f.KickoffUtc).ToList()))
                .ToList();

            // Reihenfolge: die erste K.o.-Runde nach Anstoß; jede folgende so, dass eine Partie neben
            // den beiden Vorrunden-Partien steht, aus denen ihre Teams stammen. Nur so passen die
            // Spalten optisch zusammen (Sieger fließen ins nächste Spiel derselben Höhe).
            var ordered = previousMainRound is null
                ? ties.OrderBy(tie => tie.EarliestKickoff).ToList()
                : ties
                    .OrderBy(tie => FeederIndex(tie, previousMainRound))
                    .ThenBy(tie => tie.EarliestKickoff)
                    .ToList();

            rounds.Add(new KnockoutRound(stage, StageLabel(stage), ordered));

            // Spiel um Platz 3 (Halbfinal-Verlierer) ist kein Bracket-Fortschritt → nicht als
            // Feeder-Basis fürs Finale nehmen, sonst verliert das Finale seine Zuordnung.
            if (stage != "THIRD_PLACE")
            {
                previousMainRound = ordered;
            }
        }

        return new KnockoutBracket(rounds);
    }

    private static (int, int) UnorderedPair(int a, int b) => a < b ? (a, b) : (b, a);

    // Sortierschlüssel einer Partie relativ zur Vorrunde: die kleinere Position der beiden
    // Vorrunden-Partien, aus denen ihre Teams kommen – so wird sie zwischen ihre Zubringer einsortiert.
    private static int FeederIndex(KnockoutTie tie, IReadOnlyList<KnockoutTie> previous)
    {
        var home = IndexOfParticipant(previous, tie.Home.Id);
        var away = IndexOfParticipant(previous, tie.Away.Id);

        if (home >= 0 && away >= 0)
        {
            return Math.Min(home, away);
        }

        if (home >= 0)
        {
            return home;
        }

        return away >= 0 ? away : int.MaxValue;
    }

    private static int IndexOfParticipant(IReadOnlyList<KnockoutTie> ties, int teamId)
    {
        for (var i = 0; i < ties.Count; i++)
        {
            if (ties[i].Home.Id == teamId || ties[i].Away.Id == teamId)
            {
                return i;
            }
        }

        return -1;
    }

    private static KnockoutTie BuildTie(string stage, IReadOnlyList<Fixture> legs)
    {
        var home = legs[0].Home; // Orientierung an Leg 1
        var away = legs[0].Away;
        var allPlayed = legs.All(l => l.IsPlayed);

        int? homeAggregate = null, awayAggregate = null;
        int? homePenalties = null, awayPenalties = null;

        if (allPlayed)
        {
            homeAggregate = 0;
            awayAggregate = 0;
            foreach (var leg in legs)
            {
                // Tore beider Teams über alle Legs aufsummieren, Heim/Auswärts je Leg ausrichten.
                var legHomeIsTieHome = leg.Home.Id == home.Id;
                homeAggregate += legHomeIsTieHome ? leg.HomeGoals!.Value : leg.AwayGoals!.Value;
                awayAggregate += legHomeIsTieHome ? leg.AwayGoals!.Value : leg.HomeGoals!.Value;
            }

            // Elfmeterschießen (falls vorhanden) an die Tie-Orientierung anpassen.
            if (legs.FirstOrDefault(l => l.WentToPenalties) is { } shootout)
            {
                var shootoutHomeIsTieHome = shootout.Home.Id == home.Id;
                homePenalties = shootoutHomeIsTieHome ? shootout.HomePenalties : shootout.AwayPenalties;
                awayPenalties = shootoutHomeIsTieHome ? shootout.AwayPenalties : shootout.HomePenalties;
            }
        }

        TeamRef? winner = null;
        if (allPlayed)
        {
            if (homeAggregate > awayAggregate)
            {
                winner = home;
            }
            else if (awayAggregate > homeAggregate)
            {
                winner = away;
            }
            else if (homePenalties is { } hp && awayPenalties is { } ap)
            {
                winner = hp > ap ? home : away;
            }
        }

        return new KnockoutTie(
            stage, home, away, homeAggregate, awayAggregate, homePenalties, awayPenalties,
            winner, TwoLegs: legs.Count > 1, Decided: winner is not null,
            EarliestKickoff: legs[0].KickoffUtc);
    }

    private static string StageLabel(string stage) => stage switch
    {
        "PLAYOFFS" => "Playoffs",
        "LAST_32" => "Sechzehntelfinale",
        "LAST_16" => "Achtelfinale",
        "QUARTER_FINALS" => "Viertelfinale",
        "SEMI_FINALS" => "Halbfinale",
        "THIRD_PLACE" => "Spiel um Platz 3",
        "FINAL" => "Finale",
        _ => stage
    };
}
