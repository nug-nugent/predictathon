export const enum MatchStatus {
    Pre = "Pre",
    During = "During",
    Post = "Post"
};

export const enum PredictionStatus {
    NotPredicted = "NotPredicted",
    Predicted = "Predicted",
    SavingFromNotPredicted = "SavingFromNotPredicted",
    SavingFromPredicted = "SavingFromPredicted",
    Saved = "Saved",
    SaveError = "SaveError"
};

export const enum PredictionsListStatus {
    NotLoaded = "NotLoaded",
    Loading = "Loading",
    LoadingFailed = "LoadingFailed",
    Loaded = "Loaded",
    Open = "Open"
};