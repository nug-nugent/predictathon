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
            <Text>Your role is: {user.role}</Text>
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