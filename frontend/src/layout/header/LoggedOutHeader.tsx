import { Heading, Image, Spacer } from "@chakra-ui/react";
import { Link } from "react-router";
import football from "../../assets/football.png";
import { HeaderContainer } from "./header-container/HeaderContainer";
import { LoginButton } from "./login-button/LoginButton";

export function LoggedOutHeader() {
    return (
        <HeaderContainer>
            <Link to="/">
                <Image src={football} mr={2} boxSize={{ base: "30px", md: "34px" }} />
            </Link>

            <Heading size={{ base: "2xl", md: "3xl" }}>
                <Link to="/">Predictathon</Link>
            </Heading>

            <Spacer />

            <LoginButton />
        </HeaderContainer>
    );
}
