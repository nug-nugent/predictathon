import { useEffect, useState } from "react";
import { WeekPicker } from "../../../components/week-picker/WeekPicker";
import { getPredictions } from "../../../services/predict-service";

export function PredictPage() {
    const [pageData, setPageData] = useState<any>({});

    const fetchData = async (startDate?: string) => {
        const data = await getPredictions(startDate);
        setPageData(data);
    };

    useEffect(() => {
        fetchData();
    }, []);

    console.log("page data: ", pageData);
    return (
        <>
            {pageData.availableWeeks && (
                <WeekPicker
                    weeks={pageData.availableWeeks}
                    // initialWeek should only come from the first API response so really needs its own state object.
                    initialWeek={pageData.selectedWeek}
                    onWeekChange={fetchData}
                />
            )}
        </>
    );
}
