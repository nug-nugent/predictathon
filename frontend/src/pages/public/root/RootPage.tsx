import { Link, Text } from "@chakra-ui/react";
import { Link as RouterLink } from "react-router";

export function RootPage() {
    return (
        <Text>
            Welcome! Login above or{" "}
            <Link variant={"underline"} asChild>
                <RouterLink to={"/register"}>register here</RouterLink>
            </Link>
            .
        </Text>
    );
}
