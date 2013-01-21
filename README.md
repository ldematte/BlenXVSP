BlenXVSP
========

BlenX Visual Studio Package

This solution, with several projects, creates a Visual Studio 2008 SP1 plugin which adds support for the "BlenX" language. 
BlenX is a DSL for the creation of biochemical models, inspired at Beta-binders (http://www.sciencedirect.com/science/article/pii/S1571066106004932).
The language, for which you can find an introduction here (http://link.springer.com/chapter/10.1007%2F978-1-4419-5797-9_31?LI=true), was created at The University of Trento - Microsoft Research Center for Computational and Systems Biology by Alessandro Romanel and I, with the contribution of other researchers, during our PhD.

The plugin adds intellisense, project, snippet, support execution through a stocastich simulato (BetaSIM, also written by Alessandro and I), etc. to Visual Studio. 
It uses Wintellect PowerCollections (inclued, as a DLL) and the Visual Studio Managed Package Framework for Projects (MPFProj - http://mpfproj.codeplex.com/)
They are both included, for easy of use.

I coded this plugin during my Christmas vacation in winter 2009, mostly as an exercise to explore how to use the Visual Studio SDK to extend the VS IDE; since I was working on BlenX, I decided to use it as a test language. 
However, I ended up using MPF (the Managed Package Framework), and therefore I needed to write all the parsing code from scratch (since BetaSIM was written in unmanaged C++). Fortunately, this was a quick job, and made the project self-contained. All you need to do to use it is have VS 2008 SP1 and its SDK installed, download, open the solution and recompile.

Therefore, it includes many "interesting" bits: code snippets, syntax highliting, code generation, autocompletion, parameters hints, brace matching, collapsing regions and so on. 

License(s)
----------

MPFProj is release under Microsoft Public License (Ms-PL), a liberal license similar to BSD (link: http://mpfproj.codeplex.com/license)

PowerCollections is released under the Eclipse Public License (EPL) (link: http://powercollections.codeplex.com/license)

The source code is released under the 2-clause FreeBSD license:

Copyright (c) 2009-2012, Lorenzo Dematte'
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met: 

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies, 
either expressed or implied, of the FreeBSD Project.
