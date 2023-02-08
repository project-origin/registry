# Notes from the Implementer Day 18-01-2023

### Assignments  
- @MartinSchmidt states that he will check branches and do unit tests
- @tschudid agrees to review PR [#80](https://github.com/project-origin/registry/pull/80) by @Quacktiamauct
- @wisbech onboarding of the two PhD's to the project. @wisbech is primus motor on this, hence tagged here. 
- Merging open PRs. At the time of writing, this includes: [#47](https://github.com/project-origin/registry/pull/47), [#53](https://github.com/project-origin/registry/pull/53), [#80](https://github.com/project-origin/registry/pull/80), and [#81](https://github.com/project-origin/registry/pull/81)
- @wisbech and @tpryds agree to write a paper on the implemented functionality 
- ~@lauranolling create project view for draft issues~
- ~@lauranolling Go through labels and remove `1`, `2`, `3`, `4`, `5`~
	
### Next steps 
- Performance/benchmarking tests on implementations
- Use case(s) that demonstrates that the  registry implementation works
    - Several ideas are discussed, including an initiative at ETT, and Greenlab Skive
- Try to get more participants in on the project, providing more information and progress on Github 
	
### Summary/Key Points
- Lots of great work has been done by the implementation group, and the group states that they are very close to meeting the [Alpha milestone](https://github.com/project-origin/registry/milestone/1)
- The production-team at Energinet are interested in our progress
    - The PO requested a Helm Chart - see issue #42 for progress on this
    - Members of production team are interacting here on Github - The group agrees to prioritize a dialog with them on Github.
- The ETT  Group sees value of this and is interested in our progress. 
    - At appears that all Energy Track & Trace partners have been through prototype phase, shows that it is possible, but not yet scalable - that is where Project-Origin plays in and creates some value-creation
    - It is stated that it should be prioritized to use the same API's and terminologies, to make it easy for third-party services to use our implementation. It is argued that we should use the "correct" API from the start 
- The group agrees that it is still important that the code is not just "downloaded", but that Project Origin will continue maintenance and development, and letting any interested parties participate.
- Two PhDs present some work they did on granular certification. 
    - The group finds that there are striking similarities between their and our work. 
    - The group and PhDs agree that the main difference is that Project-Origin focuses on scalability, while the PhDs' work focuses on the theoretically best setup and the ability to do so.  
- It is discussed whether to use Rust in this project, more widely than it already is. Several points are made here. 
    - Pros
        - Good interaction with other languages through bindings
			  - Rust is well-known and used in cryptography, fast in bit-manipulation and valuable in core implementation of proofs. The group agrees to keep using Rust.  
    - Cons
        - Rust can be a barrier for beginners - Currently, this barrier is removed using protobuffers as API. 
			  - Rust is relatively small, and hence it may be difficult to find wide adoption.
			  - If/when current Rust implementers moves to other projects, it may be difficult to maintain, if there are no one with Rust competencies. It is suggested to use Copilot to convert C# code to Rust, but this suggestion does not meet agreement, as the converted code can't be validated. 
- It is pointed out, that it is necessary to know who at Energinet will own the production-ready implementatoin, as some modifications and additional software is needed for identification, retrieving keys.

