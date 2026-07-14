import { Heading, Tabs, VStack } from "@chakra-ui/react";
import { AllTimeStatisticsTab } from "./AllTimeStatisticsTab";
import { CurrentCompetitionStatisticsTab } from "./CurrentCompetitionStatisticsTab";

export function StatisticsPage() {
    return (
        <VStack align="stretch" gap={4}>
            <Heading size="lg">Statistics</Heading>
            <Tabs.Root defaultValue="all-time">
                <Tabs.List>
                    <Tabs.Trigger value="all-time">All Time</Tabs.Trigger>
                    <Tabs.Trigger value="current-competition">Current Competition</Tabs.Trigger>
                </Tabs.List>
                <Tabs.Content value="all-time">
                    <AllTimeStatisticsTab />
                </Tabs.Content>
                <Tabs.Content value="current-competition">
                    <CurrentCompetitionStatisticsTab />
                </Tabs.Content>
            </Tabs.Root>
        </VStack>
    );
}
