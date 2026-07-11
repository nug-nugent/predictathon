import { Box, Heading, Link, Text } from "@chakra-ui/react";
import { useUser} from "../../../providers/UserProvider";
import { Link as RouterLink} from "react-router";

export function HomePage() {
  const { user } = useUser();

  return (
    <>
      {user ? (
        <>
          <Box>
            <Heading>Welcome, {user.name}!</Heading>
            <Text>Your roles: {user.roles.length > 0 ? user.roles.join(", ") : "Standard user"}</Text>
          </Box>
        </>
      ) : (
        <>
          <Text>Welcome! Login above or <Link variant={"underline"}><RouterLink to={"/register"}>register here</RouterLink></Link>.</Text>
        </>
      )}
    </>
  )
}