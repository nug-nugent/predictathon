import { Button, Center, Spinner, Text, VStack } from "@chakra-ui/react";
import type { ApiError } from "../../services/api";

// The standard failed-load display. Omit onRetry for contexts where retrying makes no sense.
// Pairs with useAsyncData: <ErrorState error={error} onRetry={reload} />.
export function ErrorState({ error, onRetry }: { error: ApiError; onRetry?: () => void }) {
    return (
        <Center mt={4}>
            <VStack gap={3}>
                <Text>{error.messages.join(" ")}</Text>
                {onRetry && <Button onClick={onRetry}>Try Again</Button>}
            </VStack>
        </Center>
    );
}

// The standard in-progress display while a page or widget's data loads.
export function LoadingSpinner() {
    return (
        <Center mt={4}>
            <Spinner />
        </Center>
    );
}
