import { useState } from "react";
import { Badge, Button, Dialog, HStack, Heading, Image, Link, List, Portal, Separator, Text, VStack } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";
import { Info } from "lucide-react";
import { competitionImageUrl } from "../../utils/competitionImageUrl";
import { formatDateOnly } from "../../utils/formatDateOnly";
import { Panel } from "../ui/panel";
import type { CompetitionRegistrationSummary } from "../../services/competition-registration-service";

// The pre-login landing page's spotlight card - richer than CompetitionSummaryCard (used by the
// register flow itself), with its own badge, crest, "how scoring works" explainer and embedded
// register button.
export function CompetitionSpotlightCard({ competition }: { competition: CompetitionRegistrationSummary }) {
    const imageUrl = competitionImageUrl(competition.imageFilename);
    const [scoringOpen, setScoringOpen] = useState(false);

    return (
        <Panel p={0} overflow="hidden" borderRadius="16px" boxShadow="cardRaised">
            <HStack justify="space-between" px={5} pt={4}>
                <Text fontSize="xs" fontWeight="bold" letterSpacing="wide" textTransform="uppercase" color="fg.muted">
                    Open for registration
                </Text>
                <Badge colorPalette="green" variant="subtle" borderRadius="full" px={2.5} textTransform="uppercase">Open now</Badge>
            </HStack>

            <HStack align="start" gap={4} px={5} pt={3}>
                {imageUrl && <Image src={imageUrl} alt="" boxSize="56px" objectFit="contain" flexShrink={0} />}
                <VStack align="stretch" gap={1} flex={1} minW={0}>
                    <Heading size="md">{competition.competitionName}</Heading>
                    <Text fontSize="15px" color="fg.muted">
                        {formatDateOnly(competition.startDate.slice(0, 10))} to {formatDateOnly(competition.endDate.slice(0, 10))}
                        {" · "}
                        {competition.entranceFee > 0 ? `Entry £${competition.entranceFee.toFixed(2)}` : "Free to enter"}
                    </Text>
                    {competition.information && <Text fontSize="15px" mt={1}>{competition.information}</Text>}
                </VStack>
            </HStack>

            <Link fontSize="14.5px" colorPalette="action" mx={5} mt={3} onClick={() => setScoringOpen(true)} cursor="pointer">
                <Info size={14} /> How scoring works
            </Link>

            <Button asChild colorPalette="action" h="44px" fontSize="15px" width="calc(100% - 40px)" mx={5} mt={3} mb={4}>
                <RouterLink to={`/register?competitionId=${competition.competitionID}`}>
                    Register for {competition.competitionName}
                </RouterLink>
            </Button>

            <ScoringExplainerDialog
                open={scoringOpen}
                onClose={() => setScoringOpen(false)}
                allowTwoPointers={competition.allowTwoPointers}
                entranceFee={competition.entranceFee}
            />
        </Panel>
    );
}

function ScoringExplainerDialog({
    open, onClose, allowTwoPointers, entranceFee,
}: {
    open: boolean;
    onClose: () => void;
    allowTwoPointers: boolean;
    entranceFee: number;
}) {
    return (
        <Dialog.Root open={open} onOpenChange={(e) => { if (!e.open) onClose(); }}>
            <Portal>
                <Dialog.Backdrop />
                <Dialog.Positioner>
                    <Dialog.Content>
                        <Dialog.Header>
                            <Dialog.Title>How scoring works</Dialog.Title>
                        </Dialog.Header>
                        <Dialog.Body>
                            <VStack align="stretch" gap={3}>
                                <Text>Before each match, you predict the final score. Points are awarded based on how close you get:</Text>
                                <List.Root ps={4}>
                                    <List.Item><ScorePoints value={3}>3 points</ScorePoints> - you predict the exact score.</List.Item>
                                    {allowTwoPointers ? (
                                        <>
                                            <List.Item><ScorePoints value={2}>2 points</ScorePoints> - you get the winning team and their goal tally right.</List.Item>
                                            <List.Item><ScorePoints value={1}>1 point</ScorePoints> - you get the winning team right, but not the score.</List.Item>
                                        </>
                                    ) : (
                                        <List.Item><ScorePoints value={1}>1 point</ScorePoints> - you get the winning team right, but not the score.</List.Item>
                                    )}
                                    <List.Item><ScorePoints value={0}>0 points</ScorePoints> - anything else, or no prediction made.</List.Item>
                                </List.Root>
                                <Text fontSize="sm" color="fg.muted">
                                    Ties at the end of the competition are broken by goal difference, then by the number of
                                    3-pointers scored, {allowTwoPointers && "then 2-pointers, "}and finally 1-pointers.
                                </Text>

                                {entranceFee > 0 && (
                                    <>
                                        <Separator />
                                        <VStack align="stretch" gap={1}>
                                            <Heading size="xs">Where does my money go?</Heading>
                                            <Text fontSize="sm" color="fg.muted">
                                                If there&apos;s an entry fee, there&apos;s a prize fund. All of that fee, with the
                                                exception of any PayPal fees incurred, goes into the prize fund. Cash prizes are
                                                shared among the best predictors at the end of the competition.
                                            </Text>
                                        </VStack>
                                    </>
                                )}
                            </VStack>
                        </Dialog.Body>
                        <Dialog.Footer>
                            <Button colorPalette="action" onClick={onClose}>Got it</Button>
                        </Dialog.Footer>
                    </Dialog.Content>
                </Dialog.Positioner>
            </Portal>
        </Dialog.Root>
    );
}

function ScorePoints({ value, children }: { value: 0 | 1 | 2 | 3; children: React.ReactNode }) {
    return <Text as="span" color={`points.${value}`} fontWeight="bold">{children}</Text>;
}
