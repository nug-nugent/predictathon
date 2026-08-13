import { Heading, HStack, Image, Text, VStack } from "@chakra-ui/react";
import { Link } from "react-router";
import { getHallOfFame, type HallOfFameItem } from "../../../services/hall-of-fame-service";
import { competitionImageUrl } from "../../../utils/competitionImageUrl";
import { Panel } from "../../../components/ui/panel";
import { PageHeading } from "../../../components/ui/page-heading";
import { useAsyncData } from "../../../hooks/useAsyncData";
import { ErrorState, LoadingSpinner } from "../../../components/ui/async-state";

function PlaceEntry({ label, name, userId, color }: { label: string; name: string | null; userId: string | null; color: string }) {
    if (!name) return null;

    const content = <Text as="span" fontWeight="bold" color={color}>{label}: {name}</Text>;

    return (
        <Text>
            {userId ? <Link to={`/profile/${userId}`}>{content}</Link> : content}
        </Text>
    );
}

function HallOfFameEntry({ item }: { item: HallOfFameItem }) {
    const image = competitionImageUrl(item.imageFilename);

    return (
        <HStack align="center" gap={4} py={4} borderTopWidth="1px" _first={{ borderTopWidth: 0 }}>
            {image && <Image src={image} maxH="80px" maxW="80px" alt="" />}
            <VStack align="start" gap={0} flex="1">
                <Heading size="md" color="fg.muted">{item.competitionName}</Heading>
                <PlaceEntry label="1st" name={item.winner} userId={item.winnerUserID} color="yellow.600" />
                <PlaceEntry label="2nd" name={item.secondPlace} userId={item.secondPlaceUserID} color="gray.500" />
                <PlaceEntry label="3rd" name={item.thirdPlace} userId={item.thirdPlaceUserID} color="orange.700" />
            </VStack>
        </HStack>
    );
}

export function HallOfFamePage() {
    const { data: items, error, reload } = useAsyncData(getHallOfFame, []);

    if (error) {
        return <ErrorState error={error} onRetry={reload} />;
    }

    if (items === null) {
        return <LoadingSpinner />;
    }

    return (
        <VStack align="stretch" gap={2} maxW="container.md" mx="auto">
            <PageHeading textAlign="center" mb={2}>Hall of Fame</PageHeading>
            {items.length === 0 ? (
                <Text textAlign="center" color="fg.muted">No competitions have concluded yet.</Text>
            ) : (
                <Panel accent>
                    {items.map((item) => <HallOfFameEntry key={item.hallOfFameID} item={item} />)}
                </Panel>
            )}
        </VStack>
    );
}
