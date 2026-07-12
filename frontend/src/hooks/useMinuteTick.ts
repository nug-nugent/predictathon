import { useEffect, useState } from "react";

/// Returns the current time, updated once per minute on the minute. Match prediction status
/// (Pre/During/Post, countdown text) is derived from this during render rather than stored in
/// state, so a single tick here keeps every visible match in sync without per-match timers.
export function useMinuteTick(): Date {
    const [now, setNow] = useState(() => new Date());

    useEffect(() => {
        let timer: ReturnType<typeof setTimeout>;

        const scheduleNext = () => {
            const current = new Date();
            const msToNextMinute = 60000 - (current.getTime() % 60000);

            timer = setTimeout(() => {
                setNow(new Date());
                scheduleNext();
            }, msToNextMinute);
        };

        scheduleNext();

        return () => clearTimeout(timer);
    }, []);

    return now;
}
