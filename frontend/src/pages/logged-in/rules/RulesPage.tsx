import { useEffect, useState } from "react";
import { Heading, List, Text, VStack } from "@chakra-ui/react";
import { useCompetition } from "../../../hooks/useCompetition";
import { getCompetitionDetails } from "../../../services/competition-service";
import { Panel } from "../../../components/ui/panel";
import { PageHeading } from "../../../components/ui/page-heading";

// Used for the worked examples below - not tied to any real competition's teams.
const EXAMPLE_TEAM_1 = "England";
const EXAMPLE_TEAM_2 = "Brazil";

function Points({ value, children }: { value: 0 | 1 | 2 | 3; children: React.ReactNode }) {
    return <Text as="span" color={`points.${value}`} fontWeight="bold">{children}</Text>;
}

export function RulesPage() {
    const { competitions, currentCompetitionId } = useCompetition();
    const competitionName = competitions.find((c) => c.competitionID === currentCompetitionId)?.competitionName ?? "the competition";

    // Whether this competition awards the 2-point tier (correct winner + their exact goal tally).
    // Defaults to true while loading (or with no competition selected) - it's the standard scheme;
    // the rare no-two-pointers competition swaps the bullets in once details arrive.
    const [allowTwoPointers, setAllowTwoPointers] = useState(true);

    useEffect(() => {
        if (!currentCompetitionId) return;

        getCompetitionDetails(currentCompetitionId)
            .then((details) => setAllowTwoPointers(details.allowTwoPointers))
            .catch(() => { /* best-effort - fall back to describing the standard scheme */ });
    }, [currentCompetitionId]);

    return (
        <VStack align="stretch" gap={6} maxW="container.md">
            <PageHeading textAlign="center">Rules of the game</PageHeading>

            <Panel>
                <Heading size="md" mb={2}>Scoring points</Heading>
                <Text mb={2}>
                    Players predict the score for every match in {competitionName}. They then score points based on the accuracy of those predictions.
                </Text>
                <Text mb={2}>
                    For example, if you predicted that {EXAMPLE_TEAM_1} would beat {EXAMPLE_TEAM_2} 4-2:
                </Text>
                <List.Root mb={4} ps={4}>
                    <List.Item>If the result was 4-2 you would receive the maximum <Points value={3}>3 points</Points> for a perfect prediction.</List.Item>
                    {allowTwoPointers ? (
                        <>
                            <List.Item>If the result was 4-0, 4-1, or 4-3 you would score <Points value={2}>2 points</Points> because you said that {EXAMPLE_TEAM_1} would win and score 4 goals.</List.Item>
                            <List.Item>If the result was e.g. 2-1, 3-0, 5-1, or 1-0 you would score <Points value={1}>1 point</Points>, as you correctly predicted the winning team, but not the score.</List.Item>
                        </>
                    ) : (
                        <List.Item>If the result was any other {EXAMPLE_TEAM_1} win - e.g. 4-1, 2-1, or 3-0 - you would score <Points value={1}>1 point</Points>, as you correctly predicted the winning team, but not the score.</List.Item>
                    )}
                    <List.Item>If the game ended in a draw, {EXAMPLE_TEAM_2} won, or you failed to make a prediction, you would score <Points value={0}>0 points</Points>.</List.Item>
                </List.Root>
                <Text mb={2}>
                    In another scenario, if you predicted that {EXAMPLE_TEAM_1} would draw 1-1 with {EXAMPLE_TEAM_2}:
                </Text>
                <List.Root ps={4}>
                    <List.Item>If the result was 1-1 you would receive the maximum <Points value={3}>3 points</Points> for a perfect prediction.</List.Item>
                    <List.Item>If the result was 0-0, 2-2, 3-3, 4-4, or a higher-scoring draw, you would score <Points value={1}>1 point</Points>.</List.Item>
                    <List.Item>If either team won or you failed to make a prediction, you would score <Points value={0}>0 points</Points>.</List.Item>
                </List.Root>
            </Panel>

            <Panel>
                <Heading size="md" mb={2}>Goal difference</Heading>
                <Text mb={2}>
                    Throughout the competition, your goal difference will be adversely affected by all but perfect predictions.
                </Text>
                <List.Root ps={4}>
                    <List.Item>If you predict a 2-1 home win and the result is a 2-0 home win, 1 goal will be subtracted from your goal difference.</List.Item>
                    <List.Item>If you predict a 0-2 away win and the result is a 2-0 home win, 4 goals will be subtracted from your goal difference.</List.Item>
                </List.Root>
            </Panel>

            <Panel>
                <Heading size="md" mb={2}>Deciding the victor</Heading>
                <Text mb={2}>
                    At the end of the competition, players on equal points will be separated by goal difference.
                </Text>
                <Text>
                    In the event of a tie, they'll then be separated firstly by the number of 3-pointers they've scored, {allowTwoPointers ? "then 2-pointers, " : ""}and finally 1-pointers.
                </Text>
            </Panel>
        </VStack>
    );
}
