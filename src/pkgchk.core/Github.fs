namespace pkgchk.Github

open pkgchk

type GithubComment =
    { title: string
      body: string }

    static member create title body =
        { GithubComment.title = (String.defaultValue "pkgchk summary" title)
          body = body }