import React from "react";
import { Container, SubContainer } from './styles';
import { Header } from "./Header/Header";
import { Details } from "./Details/Details";
import { Status } from "./Status/Status";

export const Match = (props) => (
    <Container showHeaders={props.showHeaders}>
        {props.showHeaders && (<Header {...props} />)}

        <SubContainer>
            <Details {...props} />
                
            <Status {...props} />
        </SubContainer>
    </Container>
);
