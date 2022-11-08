import React from "react";
import ReactDOM from "react-dom";
import { MatchList } from "../Pages/MatchList/MatchList";

document.addEventListener("DOMContentLoaded", function(event) { 
    ReactDOM.render(
        <MatchList matches={window.matches} weeks={window.weeks} loadedWeek={window.loadedWeek} imagesPath="../../Images/" />,
        document.getElementById("match-list")
    );
});
