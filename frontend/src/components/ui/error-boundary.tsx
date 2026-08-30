import { Button, Center, Heading, Text, VStack } from "@chakra-ui/react";
import { Component, type ErrorInfo, type ReactNode } from "react";

type Props = { children: ReactNode };
type State = { hasError: boolean };

// Catches render/lifecycle errors anywhere below it in the tree - without this, an unhandled
// exception (e.g. a malformed API payload reaching a .map()) unmounts the whole app to a blank
// white screen with no recovery path. Reloading (rather than just resetting `hasError`) is
// deliberate: an error boundary can't un-throw the error that caused it, so the safest recovery
// is a fresh render from a clean state.
export class ErrorBoundary extends Component<Props, State> {
    state: State = { hasError: false };

    static getDerivedStateFromError(): State {
        return { hasError: true };
    }

    componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
        console.error("Unhandled error caught by ErrorBoundary:", error, errorInfo);
    }

    render() {
        if (this.state.hasError) {
            return (
                <Center minH="100vh" p={4}>
                    <VStack gap={3}>
                        <Heading size="md">Something went wrong</Heading>
                        <Text>Sorry about that - please try reloading the page.</Text>
                        <Button onClick={() => window.location.reload()}>Reload Page</Button>
                    </VStack>
                </Center>
            );
        }

        return this.props.children;
    }
}
