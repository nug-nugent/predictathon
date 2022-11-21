import React, { useEffect, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from 'remark-gfm';
import { format, formatDistanceStrict } from "date-fns";
import { AddReactionIcon, AuthorLink, CloseIcon, Container, DateContainer, HeaderContainer, ImagePopup, MarkdownContainer, MediaContainer, MessageContainer, ProfileImage, ProfileImageContainer, ReactionsContainer, TextContainer } from "./styles";
import { Reaction } from "./Reaction/Reaction";
import { icons } from "../../Modules/icons";

export const Message = ({ id, authorImageUrl, authorUrl, authorName, date, text,
    reactions, onOpenEmojiPicker, onAddReaction, onRemoveReaction,
    imageUrl, smallImageUrl, youTubeVideoId }) => {
    const addReactionRef = useRef();
    const [messageDate] = useState(new Date(date));
    const [now, setNow] = useState(new Date());

    useEffect(() => {
        const interval = setInterval(() => setNow(new Date()), 1000);
        return () => clearInterval(interval);
    }, []);

    const reactionsMap = reactions?.reduce(function (map, reaction) {
        map[reaction.Name] = map[reaction.Name] || [];
        map[reaction.Name].push(reaction);
        return map;
    }, {});

    return (
        <Container>
            <ProfileImageContainer>
                <a href={authorUrl}>
                    <ProfileImage src={authorImageUrl} />
                </a>
            </ProfileImageContainer>
            <MessageContainer>
                <HeaderContainer>
                    <AuthorLink href={authorUrl}>{authorName}</AuthorLink>
                    <DateContainer title={format(messageDate, "PPPpp")}>
                        {` ${formatDistanceStrict(messageDate, now, { addSuffix: true })}`}
                    </DateContainer>
                    <div style={{ float: "right" }}>
                        <AddReactionIcon ref={addReactionRef} onClick={(e) => onOpenEmojiPicker(id, e.target) } />
                        {reactionsMap && Object.keys(reactionsMap).map((name) => (
                            <ReactionsContainer key={name}>
                                <Reaction messageId={id} reactions={reactionsMap[name]}
                                    onAddReaction={onAddReaction} onRemoveReaction={onRemoveReaction} />
                            </ReactionsContainer>
                        ))}
                    </div>
                </HeaderContainer>
                <TextContainer>
                    {imageUrl && (
                        <MediaContainer>
                            <ImagePopup modal lockScroll
                                trigger={<img src={smallImageUrl} />}>
                                {(close) => (
                                    <div onClick={close}>
                                        <CloseIcon fixedWidth icon={icons.close} size="2x" />
                                        <img src={imageUrl} />
                                    </div>
                                )}
                            </ImagePopup>
                        </MediaContainer>
                    )}

                    {youTubeVideoId && (
                        <MediaContainer>
                            <iframe type="text/html" src={`http://www.youtube.com/embed/${youTubeVideoId}`} frameBorder="0" />
                        </MediaContainer>
                    )}

                    <MarkdownContainer>
                        <ReactMarkdown remarkPlugins={[remarkGfm]} linkTarget="_blank">{text}</ReactMarkdown>
                    </MarkdownContainer>
                    <div style={{ clear: "both" }}></div>
                </TextContainer>
            </MessageContainer>
        </Container>
    );
};
