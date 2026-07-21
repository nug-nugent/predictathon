import { Box, HStack, Text, VStack } from "@chakra-ui/react";

type PredictedWinnerBarProps = {
    homeWin: number;
    draw: number;
    awayWin: number;
};

// Whole-number percentages that always sum to exactly 100 (largest-remainder method) - rounding
// each share independently can display a 99% or 101% total.
function toPercentages(counts: number[]): number[] {
    const total = counts.reduce((sum, c) => sum + c, 0);
    const exact = counts.map((c) => (100 * c) / total);
    const floored = exact.map(Math.floor);

    let leftover = 100 - floored.reduce((sum, p) => sum + p, 0);
    const byLargestFraction = exact
        .map((value, index) => ({ fraction: value - floored[index], index }))
        .sort((a, b) => b.fraction - a.fraction);

    for (const { index } of byLargestFraction) {
        if (leftover <= 0) break;
        floored[index] += 1;
        leftover -= 1;
    }

    return floored;
}

export function PredictedWinnerBar({ homeWin, draw, awayWin }: PredictedWinnerBarProps) {
    const total = homeWin + draw + awayWin;
    if (total === 0) {
        return null;
    }

    const [homePercent, drawPercent, awayPercent] = toPercentages([homeWin, draw, awayWin]);

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
