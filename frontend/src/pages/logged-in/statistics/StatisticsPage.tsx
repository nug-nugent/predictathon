import { Tabs, VStack } from "@chakra-ui/react";
import { AllTimeStatisticsTab } from "./AllTimeStatisticsTab";
import { AllTimeLeagueTableTab } from "./AllTimeLeagueTableTab";
import { CurrentCompetitionStatisticsTab } from "./CurrentCompetitionStatisticsTab";
import { PageHeading } from "../../../components/ui/page-heading";

export function StatisticsPage() {
    return (
        <VStack align="stretch" gap={4}>
            <PageHeading>Statistics</PageHeading>
            <Tabs.Root defaultValue="all-time">
                <Tabs.List>
                    <Tabs.Trigger value="all-time">All time</Tabs.Trigger>
                    <Tabs.Trigger value="all-time-league-table">All-time league table</Tabs.Trigger>
                    <Tabs.Trigger value="current-competition">Current competition</Tabs.Trigger>
                </Tabs.List>
                <Tabs.Content value="all-time">
                    <AllTimeStatisticsTab />
                </Tabs.Content>
                <Tabs.Content value="all-time-league-table">
                    <AllTimeLeagueTableTab />
                </Tabs.Content>
                <Tabs.Content value="current-competition">
                    <CurrentCompetitionStatisticsTab />
                </Tabs.Content>
            </Tabs.Root>
        </VStack>
    );
}
