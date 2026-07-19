import { Heading, Text, VStack } from "@chakra-ui/react";
import { formatDateOnly } from "../../utils/formatDateOnly";
import { Panel } from "../ui/panel";

type CompetitionSummary = {
    competitionName: string;
    startDate: string;
    endDate: string;
    entranceFee: number;
    information: string | null;
};

export function CompetitionSummaryCard({ competition }: { competition: CompetitionSummary }) {
    return (
        <Panel>
            <VStack align="stretch" gap={1}>
                <Heading size="md">{competition.competitionName}</Heading>
                <Text fontSize="sm" color="fg.muted">
                    {formatDateOnly(competition.startDate.slice(0, 10))} to {formatDateOnly(competition.endDate.slice(0, 10))}
                </Text>
                <Text fontSize="sm">
                    {competition.entranceFee > 0 ? `Entry fee: £${competition.entranceFee.toFixed(2)}` : "Free to enter"}
                </Text>
                {competition.information && <Text fontSize="sm" mt={2}>{competition.information}</Text>}
            </VStack>
        </Panel>
    );
}
