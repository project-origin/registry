# Decision-Making Policy 

## Table of Contents

1. [Introduction](#introduction)
2. [Issues](#issues)
    - [Task](#task)
    - [Feature Request](#feature-request)
    - [Bug Report](#bug-report)
6. [Pull requests](#pull-requests)
    - [Creating a Meeting Minutes](#creating-a-request-for-comments)
    - [Creating a Request for Comments](#creating-a-request-for-comments)
9. [Discussions](#discussions)
    - [Announcements](#announcements)
    - [General](#general)
    - [Ideas](#ideas)
    - [Polls](#polls)
    - [Q & A](#q--a)
    - [Show and Tell](#show-and-tell)


## Introduction 
This decision-making policy is mostly created as a collection of practices, reflecting what the community in this repository finds as best practices for making the project run smoothly. Given that this is a dynamic, evolving project, such practices will of course change over time. 
Hence, If you have a specific suggestion for modifying or reformulating something in this policy, feel free to [create a Pull Request], containing your suggestion(s). 

In Project-Origin, work is mostly done asynchronously, where normal decision-making structures tend to be ineffective. 
Hence, we have set up this decision-making policy, to create some general guiding structures to follow, specifying when a suggestion or vote can be regarded as decided/done. 
The intent of this policy is to explicitly give the decision mandate to the community actively participating in this repository. 

In some cases, this decision policy will lead to decisions being made, that you don't necessarily agree with. 
In such case, you can always pose an alternative suggestion, following our [Contribution Guidelines], for the community to adress. 
Of course this is not a guarantee that the community will agree with you.

In any case of interaction on theProject-Origin Github, the Project-Origin working group expects participants in the community to follow our [Contribution Guidelines] and behave according to our [Code of Conduct].
If any violating behavior is encountered, the [Trusted Committer] has the the authority to require modifications and/or ultimately remove the violating content on behalf of the Project-Origin working group. 
This decision-making policy is created specifically for the _origin-collaboration_ repository. 


&nbsp;

## Issues
Everyone in the community can create issues, using one of the [issue templates] provided, or add comments and suggestions to others' issues posted here in this repository. Issues are meant for discussing specific, closed-form ideas or details of a project. For more details on the use of issues, see the [Contribution Guidelines]. 

Generally, a decision on an issue can be considered as made, when:
1. The community has had reasonable time to react and provide input to the issue.
  1a. Issues are raised with reasonably descriptive titles, attached to the respective boards and likely relevant people are tagged. At the same time, participants will review the boards and issues for new things regularly. (like an inbox).
2. The issue is closed, with a concluding comment on the reason for closing the issue. The owner of the issue is responsible for providing such comment, and closing the issue.
3. If relevant, a pull request is created, documenting the decision, in a location that is easy to find.

Decision-making for issues depend on the type of issue, and will be elaborated for the different types of issues below.

### Task 
The [Task] issue informs the community of a task that you, as a contributor, are working on. It helps the community keep up to date with your progress, and creates a space for you to receive inputs and feedback on your task. If you actively need external inputs to progress with your task, mark the issue with the label `help wanted` and tag anyone you want inputs/feedback from. Anyone can provide their inputs to the issue if relevant.
Hence, in the case of a Task, the decision-making mandate belongs to the Contributor, but with the expectation of the contributor taking the community's inputs into account in their work. 
However, if there are any related pull requests, these will require acceptance from (one of) the [Trusted Committer](s). This will be addressed in the [Pull Requests section](#pull-requests). 

#### _Timeframe_
If receiving inputs to your task is urgent, mark your issue with the labels `urgent` and `help wanted`, and tag anyone you need help from. However, please use these with consideration. If the tags are overused/misused, they will take attention away from the issues that do require immediate response.

### Feature Request
The [Feature Request] issue is used for specific ideas and improvement suggestions. 
Everyone from the community can provide inputs to the feature request.
In the spirit of Open Source and InnerSource the focus lies not on merely requesting but also in doing (most of) the work that has been requested as a requester yourself with the supervision and some guidance of the [Trusted Committer]s once there is enough agreement on the feature. 

Any suggestions and ideas posted in this repository are appreciated. 
It is a core principle of Project-Origin that the decision-mandate belongs to the community, and the [Trusted Committer](s) will always collaborate with the Contributor and community to find a good solution. 
Hence, The [Trusted Committer](s) will provide feedback on potential adjustments needed, to harmonize the feature with the overall direction and goals of the project. 
When the suggestion reaches a stage where it can be implemented, you can [create a Pull Request], and the work can be continued there. 

Note that, ultimately [the Trusted Committer] has the mandate to reject a feature request, or course of action, if they deem the suggested feature not to serve the best for the project. 

#### _Timeframe_
Feature requests do not have a timeframe. If you find that implementing your suggested feature is urgent, making sure to define your suggestion thoroughly and according with [Contribution Guidelines] can help avoid long processing and modification times, and hence accelerate implementation. 
You can and should also consider to offer to do the implementation (as a pull request) yourself to speed things up. The [Trusted Commiter]s will be threre to mentor you on your way.
The Trusted Committer(s) will always do their best to react fast. If the Trusted Committer doesn't receive any responses from the [Contributor] in an extended period of time, they have the mandate to close the issue containing the feature request. 

### Bug Report
A [Bug Report] is used to inform the community of an error or problem encountered in the project. It allows the community to discuss the problem, and find potential solutions. 
When the community finds agreement on how to fix the bug, you can [create a Pull Request], and the bug can be fixed from there. 
It is customary to directly offer your fix for the problem as a pull request for review to facilitate and speed up the solution of the problem. Even if not perfect, this can also advance the discussion by discussing it more specifically.
However, note that ultimately, [the Trusted Committer] has the mandate to reject a suggested/agreed upon course of action, if it is deemed not to serve the best for the project. 

#### _Timeframe_
Similar to feature requests, there is not a timeframe on bug reports. If fixing an error is urgent to you, make sure to put some time and effort into defining the error thoroughly and into finding a solution with the rest of the community.
You can also offer your own version of a fix in a pull request for the bug to facilitate and speed up the solution process. 
If [the Trusted Committer] doesn't receive any responses from the Contributor in an extended period of time, they have the mandate to close the issue containing the bug report. 

## Pull requests 
Everyone in the community can [create a Pull Request], or add reviews, comments, and suggestions to existing [Pull Requests]. 
The process for _merging_ pull requests follows the [InnerSource approach]https://github.com/project-origin/origin-collaboration/blob/main/docs/introductory/innersource-short-role-descriptions.md#what-is-the-working-process-using-the-three-roles), where the decision of which and when pull requests get merged belongs to the [Trusted Committer]. 

#### _Timeframe_
To ensure quick merge turnarounds, the merging process for pull requests generally follows a [lazy consensus] approach, which means that community members not reacting to a pull request give their silent consent. 
However, to ensure code quality and consistency with the goals of Project-Origin, **a pull request can only be merged when it is approved by a [Trusted Committer]**. To make sure you fulfill any merging criteria, make sure to consult the [Contribution Guidelines] before creating the pull request. 

### Creating a Meeting Minutes
There are no specific decision processes connected to submitting a meeting minutes pull request. Any decision-making processes should be specified by the submitter of the minutes pull request, or by the meeting participants if relevant. However, it is good practice as a facilitator to submit the meeting minutes pull request in good time, to allow meeting participants to add any inputs or comments before the upcoming meeting. 

### Creating a Request for Comments
While feature requests are used to define specific, smaller ideas, a [Request for Comments] (RFC) is used to suggest broader, more substantial changes, such as new governance structures, new documentation setups, or new formats of bigger functionalities. You can learn more about how to submit an RFC in the [Contribution Guidelines].

Since the RFCs entails more significant changes to the project, it is ultimately up to the working group of Project-Origin to approve an RFC. 

#### _Timeframe_
There is not a timeframe for RFCs. The Trusted Committer(s) will always do their best to reply fast. 
If the Trusted Committer doesn't receive any responses from the Contributor in an extended period of time, they have the mandate to close the issue. 

&nbsp;

## Discussions
[Discussions] are intended for open-form ideas or topics, and creates a space for the community to interact and communicate more broadly than is possible in the more closed-form issues and pull requests. 
Given the more informal format of Discussions, the decision-making mostly revolves around potentially creating issues and pull requests for ideas that sees a large connect from the community and evolves to a more definite feature. 
The only exception is the [Polls] channel, where decision-making is inherent, and hence somewhat more involved. 

Decision-making will be adressed individually for each discussion channel in the following.

### Announcements
[Announcements] won't be used for decision-making, but can be used to announce, e.g., any decisions made in the working group, or any major feature or progress that has been made, that affects the community.

### General
[General] is a forum for open, informal discussion in the community. This forum won't include decision-making.

### Ideas
[Ideas] gives the community a place to discuss loose, open ideas, and potentially develop an idea until it becomes a well-defined feature request. 
If an idea evolves to a feature request, you can [create an Issue] for this.  

### Polls
[Polls] will also be posted in the [Discussions].
The decision-making in polls follows a [lazy consensus] approach, inspired by the [Apache definition](https://community.apache.org/committers/lazyConsensus.html) of the concept. 
A poll can be either _open_ or _closed_. When a poll is _closed_, the outcome of the vote indicated in the poll can be considered as decided on, unless the poll is closed as "outdated" or "duplicate". 


#### _Decision Criteria_
In this repository the lazy consensus approach entails that silence, or not answering the poll, is consent to whatever gets most votes. A number of specific criteria are set up for polls:

- If no votes are received, the [default option] will be chosen.
- If two or more options get the same number of votes, the decision-mandate is given to the working group of Project-Origin. The working group will then choose the decision they find as the In such case, the [Trusted Committer] will notify the community of the decision.
- If you have any adjustment requests or comments to the poll, provide your feedback in the comment section on the poll. 
- If you do not agree with the result of a poll, that is considered as closed, you must actively propose an alternative following our [Contribution Guidelines].  

#### _Timeframe_
The answering deadline should be reasonable, to ensure that the community has the chance to see and react on your poll. It is recommended to provide **a voting time frame on at least 10 working days** from the date that the poll receives the _open_ status. This time frame allows for interaction and potentially discussion on and adjustment of the poll. 


**Urgent decision-making**

If you find that making a decision on your poll is urgent, make sure to label the poll as `urgent`, and tag anyone you know is directly affected by the decision. However, note that the Trusted Committer must accept the proposed poll, until its status converts to _open_.

Sometimes a working group (e.g., in one of the [Weekly Synchronous Sessions]) can evaluate decision-making on a topic as urgent in order to ensure continued progress of the project. In such cases, the participants can make a decision fast, after which one person from the working group is responsible for documenting the decision made by the group, and to tag anyone directly affected by the decision. Note that ultimately the Trusted Committer has the mandate to accept the decision on behalf of the health of the project and its progress.

### Q & A
[Q & A] creates a space for the community to ask each other questions, and support each other on specific problems encountered. In a Q & A post, the author of the post has the mandate to mark something as an answer. However, any community member is free to suggest/object against an answer. If any task or feature request arises from the discussion, you can [create an Issue] or [create a Pull Request] for the task or feature request. 

### Show and Tell 
[Show and Tell] is not used for decision-making, but rather as a place to highlight and praise contributions that you, or someone else have made. 


<!-- Anchorlinks --> 

[Trusted Committer]: https://github.com/project-origin/origin-collaboration/blob/main/docs/introductory/innersource-short-role-descriptions.md#the-trusted-committer
[Contribution Guidelines]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md
[lazy consensus]: https://community.apache.org/committers/lazyConsensus.html
[create a Pull Request]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#creating-a-pull-request
[create an Issue]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#issues


[Pull Requests]: https://github.com/project-origin/origin-collaboration/pulls
[Issues]: https://github.com/project-origin/origin-collaboration/issues
[Discussions]: https://github.com/project-origin/origin-collaboration/discussions

[General]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#general
[Announcements]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#announcements
[Ideas]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#ideas
[Polls]: https://github.com/project-origin/origin-collaboration/discussions/categories/polls
[Q & A]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#q--a
[Show and Tell]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#show-and-tell

[default option]: https://github.com/project-origin/origin-collaboration/issues
[Weekly Synchronous Sessions]: https://github.com/project-origin/origin-collaboration#weekly-synchronous-sessions
[Code of Conduct]: https://github.com/project-origin/.github/blob/main/CODE_OF_CONDUCT.md
[issue templates]: https://github.com/project-origin/origin-collaboration/issues/new/choose
[Contributor]: https://github.com/project-origin/origin-collaboration/blob/main/docs/introductory/innersource-short-role-descriptions.md#the-contributor
[the Trusted Committer]: https://github.com/project-origin/origin-collaboration#need-any-help

[Task]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#task
[Feature Request]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#feature-request
[Bug Report]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/contribution_guidelines.md#bug-report

[Request for Comments]: https://github.com/project-origin/origin-collaboration/blob/main/docs/additional_reading/rfc.md
[Meeting Minutes template]: https://github.com/project-origin/origin-collaboration/blob/main/pull_request_templates/meeting-minutes-template.md
[Request for Comments template]: https://github.com/project-origin/origin-collaboration/blob/main/pull_request_templates/rfc_template.md
