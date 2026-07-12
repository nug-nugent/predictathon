import { Box, HStack, Text, VStack } from "@chakra-ui/react";

type PredictedWinnerBarProps = {
    homeWin: number;
    draw: number;
    awayWin: number;
};

export function PredictedWinnerBar({ homeWin, draw, awayWin }: PredictedWinnerBarProps) {
    const total = homeWin + draw + awayWin;
    if (total === 0) {
        return null;
    }

    const homePercent = Math.round((100 * homeWin) / total);
    const drawPercent = Math.round((100 * draw) / total);
    const awayPercent = Math.round((100 * awayWin) / total);

    return (
        <VStack gap={1} w="100%">
            <HStack w="100%" h="8px" rounded="full" overflow="hidden" gap={0}>
                <Box w={`${homePercent}%`} h="100%" bg="green.400" />
                <Box w={`${drawPercent}%`} h="100%" bg="gray.400" />
                <Box w={`${awayPercent}%`} h="100%" bg="blue.400" />
            </HStack>
            <HStack fontSize="xs" justify="space-between" w="100%">
                <Text><Text as="span" fontWeight="bold">{homePercent}%</Text> ({homeWin})</Text>
                <Text>Draw: <Text as="span" fontWeight="bold">{drawPercent}%</Text> ({draw})</Text>
                <Text><Text as="span" fontWeight="bold">{awayPercent}%</Text> ({awayWin})</Text>
            </HStack>
        </VStack>
    );
}
