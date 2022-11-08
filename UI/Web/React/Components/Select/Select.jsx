import React from "react";
import { Container, SelectElement, SelectIcon } from "./styles";

export const Select = ({ value, onChange, children}) => (
    <Container>
        <SelectElement value={value} onChange={onChange}>
            {children}
        </SelectElement>
        <SelectIcon />
    </Container>
)
