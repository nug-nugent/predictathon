import styled from "styled-components";
import { media } from "../../../Modules/theme";

export const Container = styled.div`
    padding: 10px 20px;

    ${media.mobile} {
        font-size: 0.9em;
        padding: 10px;
    }
`;

export const MostPredictedContainer = styled.div`
    display: flex;
    justify-content: space-between;
`;